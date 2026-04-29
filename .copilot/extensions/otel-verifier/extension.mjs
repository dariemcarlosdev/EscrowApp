// NexSynapse OTel Verifier Extension
// Portable OpenTelemetry verification for model routing trust.
// Proves which LLM actually handled each sub-agent via OTel trace analysis.
//
// Tools: otel_setup, otel_status, otel_verify
// v1: JSONL file-based traces only. No protobuf/OTLP parsing.

import { joinSession } from "@github/copilot-sdk/extension";
import { readFile, access, stat } from 'node:fs/promises';
import { constants } from 'node:fs';
import { join, resolve, isAbsolute, dirname } from 'node:path';
import { platform, homedir, EOL } from 'node:os';
import { execFile } from 'node:child_process';

// ── Constants ──────────────────────────────────────────────────────────

const TRACE_ENV_VAR = 'COPILOT_OTEL_FILE_EXPORTER_PATH';
const OTEL_ENABLED_VAR = 'COPILOT_OTEL_ENABLED';
const OTLP_ENDPOINT_VAR = 'OTEL_EXPORTER_OTLP_ENDPOINT';
const CONTENT_CAPTURE_VAR = 'OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT';

const DEFAULT_TRACE_PATH_WIN = join(homedir(), '.copilot', 'otel-traces.jsonl');
const DEFAULT_TRACE_PATH_UNIX = join(homedir(), '.copilot', 'otel-traces.jsonl');

const GITIGNORE_PATTERN = '*.jsonl';

// ── Helpers ────────────────────────────────────────────────────────────

function getOS() {
  const p = platform();
  if (p === 'win32') return 'windows';
  if (p === 'darwin') return 'macos';
  return 'linux';
}

async function fileExists(p) {
  try { await access(p, constants.R_OK); return true; } catch { return false; }
}

async function dirWritable(p) {
  try { await access(p, constants.W_OK); return true; } catch { return false; }
}

async function getFileSize(p) {
  try { const s = await stat(p); return s.size; } catch { return 0; }
}

async function getFileMtime(p) {
  try { const s = await stat(p); return s.mtime; } catch { return null; }
}

function dockerAvailable() {
  return new Promise((res) => {
    execFile('docker', ['info', '--format', '{{.ServerVersion}}'], {
      timeout: 5000, windowsHide: true,
    }, (err, stdout) => {
      if (err) return res({ available: false, reason: err.code === 'ENOENT' ? 'Docker not installed' : 'Docker daemon not running' });
      res({ available: true, version: (stdout || '').trim() });
    });
  });
}

function defaultTracePath() {
  return getOS() === 'windows' ? DEFAULT_TRACE_PATH_WIN : DEFAULT_TRACE_PATH_UNIX;
}

// ── Span Tree Builder ──────────────────────────────────────────────────
// Uses parentSpanId/spanId for proper correlation (not just traceId).

function buildSpanTree(spans) {
  const byId = new Map();
  const roots = [];

  for (const span of spans) {
    const node = { span, children: [] };
    byId.set(span.spanId, node);
  }

  for (const [id, node] of byId) {
    const parentId = node.span.parentSpanId;
    if (parentId && byId.has(parentId)) {
      byId.get(parentId).children.push(node);
    } else {
      roots.push(node);
    }
  }

  return { roots, byId };
}

function extractModelFromSpan(span) {
  // Primary: explicit GenAI attribute (supports both flat object and array-of-kv formats)
  if (span.attributes) {
    if (Array.isArray(span.attributes)) {
      for (const attr of span.attributes) {
        if (attr.key === 'gen_ai.request.model' || attr.key === 'gen_ai.response.model') {
          return attr.value?.stringValue || attr.value?.string_value || String(attr.value);
        }
      }
    } else {
      const model = span.attributes['gen_ai.request.model'] || span.attributes['gen_ai.response.model'];
      if (model) return String(model);
    }
  }
  // Fallback: parse from span name "chat <model>"
  if (span.name && span.name.startsWith('chat ')) {
    return span.name.replace(/^chat\s+/, '');
  }
  return null;
}

function extractAttrValue(span, key) {
  if (!span.attributes) return null;
  // Flat object format: { "key": value }
  if (!Array.isArray(span.attributes)) {
    const val = span.attributes[key];
    return val !== undefined ? val : null;
  }
  // Array-of-kv format: [{ key, value: { stringValue } }]
  for (const attr of span.attributes) {
    if (attr.key === key) {
      return attr.value?.stringValue || attr.value?.intValue || attr.value?.doubleValue
        || attr.value?.string_value || attr.value?.int_value || attr.value?.double_value
        || String(attr.value);
    }
  }
  return null;
}

function spanDurationMs(span) {
  // HrTime tuple format: startTime: [seconds, nanoseconds]
  if (Array.isArray(span.startTime) && Array.isArray(span.endTime)) {
    const startNs = BigInt(span.startTime[0]) * 1_000_000_000n + BigInt(span.startTime[1]);
    const endNs = BigInt(span.endTime[0]) * 1_000_000_000n + BigInt(span.endTime[1]);
    return Number(endNs - startNs) / 1_000_000;
  }
  // OTLP protobuf format: startTimeUnixNano as string
  if (span.startTimeUnixNano && span.endTimeUnixNano) {
    return (Number(BigInt(span.endTimeUnixNano) - BigInt(span.startTimeUnixNano))) / 1_000_000;
  }
  return null;
}

function spanStartTime(span) {
  // HrTime tuple format: startTime: [seconds, nanoseconds]
  if (Array.isArray(span.startTime)) {
    return new Date(span.startTime[0] * 1000 + Math.floor(span.startTime[1] / 1_000_000));
  }
  // OTLP protobuf format
  if (span.startTimeUnixNano) {
    return new Date(Number(BigInt(span.startTimeUnixNano) / 1_000_000n));
  }
  return null;
}

// ── JSONL Parser ───────────────────────────────────────────────────────
// Handles Copilot CLI HrTime format, direct span objects, and batched resourceSpans.

function parseJsonlSpans(content) {
  const spans = [];
  const lines = content.split('\n');

  for (const line of lines) {
    if (!line.trim()) continue;
    try {
      const obj = JSON.parse(line);

      // Batched OTel format: { resourceSpans: [{ scopeSpans: [{ spans: [...] }] }] }
      if (obj.resourceSpans) {
        for (const rs of obj.resourceSpans) {
          for (const ss of (rs.scopeSpans || rs.scope_spans || [])) {
            for (const span of (ss.spans || [])) {
              spans.push(span);
            }
          }
        }
      }
      // Copilot CLI format: { type: "span", spanId, startTime: [s, ns], attributes: {} }
      else if (obj.type === 'span' && obj.spanId) {
        spans.push(obj);
      }
      // Generic direct span object (snake_case or camelCase)
      else if (obj.spanId || obj.span_id) {
        spans.push({
          traceId: obj.traceId || obj.trace_id,
          spanId: obj.spanId || obj.span_id,
          parentSpanId: obj.parentSpanId || obj.parent_span_id,
          name: obj.name,
          startTime: obj.startTime,
          endTime: obj.endTime,
          startTimeUnixNano: obj.startTimeUnixNano || obj.start_time_unix_nano,
          endTimeUnixNano: obj.endTimeUnixNano || obj.end_time_unix_nano,
          attributes: obj.attributes,
          status: obj.status,
          kind: obj.kind,
          events: obj.events,
          resource: obj.resource,
        });
      }
    } catch {
      // Skip malformed lines — partial writes during active session
    }
  }

  return spans;
}

// ── Tool: otel_setup ───────────────────────────────────────────────────

async function otelSetup({ profile = 'file', trace_path, endpoint, shell }) {
  const os = getOS();
  const detectedShell = shell || (os === 'windows' ? 'powershell' : 'bash');
  const tracePath = trace_path || defaultTracePath();
  const traceDir = dirname(tracePath);
  const writable = await dirWritable(traceDir);

  const lines = [];
  lines.push('# 🔧 OTel Verification Setup');
  lines.push(`**Profile:** ${profile} | **OS:** ${os} | **Shell:** ${detectedShell}`);
  lines.push('');

  // Validate target directory
  if (profile === 'file' && !writable) {
    lines.push(`> ⚠️ Directory \`${traceDir}\` is not writable. Create it first or choose a different path.`);
    lines.push('');
  }

  // Docker check for Jaeger profile
  if (profile === 'jaeger') {
    const docker = await dockerAvailable();
    if (!docker.available) {
      lines.push(`> ⚠️ Docker: ${docker.reason}. Jaeger profile requires a running Docker daemon.`);
      lines.push('');
    } else {
      lines.push(`> ✅ Docker daemon v${docker.version} detected.`);
      lines.push('');
    }
  }

  // Privacy warning
  lines.push('## ⚠️ Privacy Notice (Fintech)');
  lines.push('');
  lines.push('OTel traces may contain **model names, token counts, and timing data**.');
  lines.push('Content capture (`OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`) is **OFF by default**.');
  lines.push('Enabling it records **full prompts and responses** — never enable on production data.');
  lines.push(`Trace files should be **gitignored** — add \`${GITIGNORE_PATTERN}\` to \`.gitignore\` if not present.`);
  lines.push('');

  // Generate shell-specific commands
  lines.push('## Setup Commands');
  lines.push('');
  lines.push('Copy-paste the block matching your shell, then **restart** Copilot CLI:');
  lines.push('');

  const snippets = generateShellSnippets(profile, tracePath, endpoint);

  if (detectedShell === 'powershell' || os === 'windows') {
    lines.push('### PowerShell');
    lines.push('```powershell');
    lines.push(snippets.powershell);
    lines.push('```');
    lines.push('');
    lines.push('### cmd.exe');
    lines.push('```cmd');
    lines.push(snippets.cmd);
    lines.push('```');
  }

  if (detectedShell !== 'powershell' || os !== 'windows') {
    lines.push('### bash / zsh');
    lines.push('```bash');
    lines.push(snippets.bash);
    lines.push('```');
    lines.push('');
    lines.push('### fish');
    lines.push('```fish');
    lines.push(snippets.fish);
    lines.push('```');
  }

  if (profile === 'jaeger') {
    lines.push('');
    lines.push('### Start Jaeger (run before restarting Copilot CLI)');
    lines.push('```bash');
    lines.push('docker run -d --name jaeger \\');
    lines.push('  -p 4317:4317 -p 4318:4318 -p 16686:16686 \\');
    lines.push('  jaegertracing/all-in-one:latest');
    lines.push('```');
    lines.push(`Open Jaeger UI: [http://localhost:16686](http://localhost:16686)`);
  }

  lines.push('');
  lines.push('## After Restart');
  lines.push('');
  lines.push('1. Run `otel_status` to confirm OTel is active');
  lines.push('2. Execute a multi-agent task (e.g., launch explore/task agents)');
  lines.push('3. Run `otel_verify` to analyze which models handled each agent');

  return lines.join('\n');
}

function generateShellSnippets(profile, tracePath, endpoint) {
  const absTracePath = isAbsolute(tracePath) ? tracePath : resolve(tracePath);

  if (profile === 'file') {
    return {
      powershell: [
        `$env:${TRACE_ENV_VAR} = "${absTracePath}"`,
        `$env:${OTEL_ENABLED_VAR} = "true"`,
        `# Then restart: copilot`,
      ].join('\n'),
      cmd: [
        `set ${TRACE_ENV_VAR}=${absTracePath}`,
        `set ${OTEL_ENABLED_VAR}=true`,
        `REM Then restart: copilot`,
      ].join('\n'),
      bash: [
        `export ${TRACE_ENV_VAR}="${absTracePath}"`,
        `export ${OTEL_ENABLED_VAR}=true`,
        `# Then restart: copilot`,
      ].join('\n'),
      fish: [
        `set -x ${TRACE_ENV_VAR} "${absTracePath}"`,
        `set -x ${OTEL_ENABLED_VAR} true`,
        `# Then restart: copilot`,
      ].join('\n'),
    };
  }

  if (profile === 'jaeger') {
    const ep = endpoint || 'http://localhost:4318';
    return {
      powershell: [
        `$env:${OTLP_ENDPOINT_VAR} = "${ep}"`,
        `$env:${OTEL_ENABLED_VAR} = "true"`,
        `# Then restart: copilot`,
      ].join('\n'),
      cmd: [
        `set ${OTLP_ENDPOINT_VAR}=${ep}`,
        `set ${OTEL_ENABLED_VAR}=true`,
        `REM Then restart: copilot`,
      ].join('\n'),
      bash: [
        `export ${OTLP_ENDPOINT_VAR}="${ep}"`,
        `export ${OTEL_ENABLED_VAR}=true`,
        `# Then restart: copilot`,
      ].join('\n'),
      fish: [
        `set -x ${OTLP_ENDPOINT_VAR} "${ep}"`,
        `set -x ${OTEL_ENABLED_VAR} true`,
        `# Then restart: copilot`,
      ].join('\n'),
    };
  }

  if (profile === 'otlp') {
    const ep = endpoint || 'http://localhost:4318';
    return {
      powershell: [
        `$env:${OTLP_ENDPOINT_VAR} = "${ep}"`,
        `$env:${OTEL_ENABLED_VAR} = "true"`,
        `# Then restart: copilot`,
      ].join('\n'),
      cmd: [
        `set ${OTLP_ENDPOINT_VAR}=${ep}`,
        `set ${OTEL_ENABLED_VAR}=true`,
        `REM Then restart: copilot`,
      ].join('\n'),
      bash: [
        `export ${OTLP_ENDPOINT_VAR}="${ep}"`,
        `export ${OTEL_ENABLED_VAR}=true`,
        `# Then restart: copilot`,
      ].join('\n'),
      fish: [
        `set -x ${OTLP_ENDPOINT_VAR} "${ep}"`,
        `set -x ${OTEL_ENABLED_VAR} true`,
        `# Then restart: copilot`,
      ].join('\n'),
    };
  }

  return { powershell: '# Unknown profile', cmd: 'REM Unknown profile', bash: '# Unknown profile', fish: '# Unknown profile' };
}

// ── Tool: otel_status ──────────────────────────────────────────────────

async function otelStatus() {
  const lines = [];
  lines.push('# 📡 OTel Verification Status');
  lines.push('');

  // Check env vars
  const fileExporter = process.env[TRACE_ENV_VAR] || null;
  const otelEnabled = process.env[OTEL_ENABLED_VAR] || null;
  const otlpEndpoint = process.env[OTLP_ENDPOINT_VAR] || null;
  const contentCapture = process.env[CONTENT_CAPTURE_VAR] || null;

  lines.push('## Environment Variables');
  lines.push('');
  lines.push(`| Variable | Value | Status |`);
  lines.push(`|---|---|---|`);
  lines.push(`| \`${OTEL_ENABLED_VAR}\` | ${otelEnabled || '—'} | ${otelEnabled === 'true' ? '✅ Active' : '❌ Not set'} |`);
  lines.push(`| \`${TRACE_ENV_VAR}\` | ${fileExporter || '—'} | ${fileExporter ? '✅ File export' : '⬜ Not set'} |`);
  lines.push(`| \`${OTLP_ENDPOINT_VAR}\` | ${otlpEndpoint || '—'} | ${otlpEndpoint ? '✅ OTLP endpoint' : '⬜ Not set'} |`);
  lines.push(`| \`${CONTENT_CAPTURE_VAR}\` | ${contentCapture || '—'} | ${contentCapture === 'true' ? '⚠️ CONTENT CAPTURE ON' : '✅ Off (safe)'} |`);
  lines.push('');

  // Privacy alert
  if (contentCapture === 'true') {
    lines.push('> 🔴 **PRIVACY ALERT:** Content capture is **ON**. Full prompts and responses are being recorded.');
    lines.push('> This may include PII, payment data, or secrets. Disable for fintech workloads.');
    lines.push('');
  }

  // Overall status
  const isActive = otelEnabled === 'true' || !!fileExporter || !!otlpEndpoint;
  const exportMode = fileExporter ? 'File (JSONL)' : otlpEndpoint ? `OTLP (${otlpEndpoint})` : 'None';

  lines.push('## Pipeline Status');
  lines.push('');
  lines.push(`| Check | Result |`);
  lines.push(`|---|---|`);
  lines.push(`| OTel Active | ${isActive ? '✅ Yes' : '❌ No — run `otel_setup` first'} |`);
  lines.push(`| Export Mode | ${exportMode} |`);

  // File-specific checks
  if (fileExporter) {
    const exists = await fileExists(fileExporter);
    const size = exists ? await getFileSize(fileExporter) : 0;
    const mtime = exists ? await getFileMtime(fileExporter) : null;
    const sizeKB = (size / 1024).toFixed(1);
    const age = mtime ? `${Math.round((Date.now() - mtime.getTime()) / 60000)}min ago` : 'N/A';

    lines.push(`| Trace File Exists | ${exists ? '✅ Yes' : '⚠️ No (will be created on first CLI run)'} |`);
    lines.push(`| Trace File Size | ${sizeKB} KB |`);
    lines.push(`| Last Modified | ${age} |`);

    // Estimate span count
    if (exists && size > 0) {
      try {
        const content = await readFile(fileExporter, 'utf-8');
        const lineCount = content.split('\n').filter(l => l.trim()).length;
        lines.push(`| JSONL Lines | ${lineCount} |`);
      } catch { /* skip */ }
    }
  }

  // Docker check (for Jaeger users)
  const docker = await dockerAvailable();
  lines.push(`| Docker Daemon | ${docker.available ? `✅ v${docker.version}` : `⬜ ${docker.reason}`} |`);
  lines.push('');

  if (!isActive) {
    lines.push('## Next Steps');
    lines.push('');
    lines.push('Run `otel_setup` with profile `file`, `jaeger`, or `otlp` to generate setup commands.');
  }

  return lines.join('\n');
}

// ── Tool: otel_verify ──────────────────────────────────────────────────

async function otelVerify({ trace_path, since_minutes, trace_id }) {
  const lines = [];
  const tracePath = trace_path || process.env[TRACE_ENV_VAR] || defaultTracePath();

  lines.push('# 🔍 Model Verification Report');
  lines.push('');

  // Validate trace file
  if (!(await fileExists(tracePath))) {
    lines.push(`❌ Trace file not found: \`${tracePath}\``);
    lines.push('');
    lines.push('Run `otel_setup` to configure OTel, restart Copilot CLI, execute tasks, then re-run `otel_verify`.');
    return lines.join('\n');
  }

  const fileSize = await getFileSize(tracePath);
  if (fileSize === 0) {
    lines.push(`⚠️ Trace file is empty: \`${tracePath}\``);
    lines.push('');
    lines.push('Start a Copilot CLI session with OTel enabled, run some tasks, then re-run `otel_verify`.');
    return lines.join('\n');
  }

  // Parse spans
  const content = await readFile(tracePath, 'utf-8');
  let spans = parseJsonlSpans(content);

  if (spans.length === 0) {
    lines.push('⚠️ No OTel spans found in the trace file. Format may be unsupported (v1 supports JSONL only).');
    return lines.join('\n');
  }

  // ── Session scoping ──────────────────────────────────────────────
  // Prevents mixing traces from different CLI sessions.

  const scopeLabel = [];
  const originalCount = spans.length;

  if (trace_id) {
    spans = spans.filter(s => s.traceId === trace_id);
    scopeLabel.push(`traceId=${trace_id}`);
  }

  if (since_minutes && since_minutes > 0) {
    const cutoff = Date.now() - (since_minutes * 60 * 1000);
    spans = spans.filter(s => {
      const t = spanStartTime(s);
      return t && t.getTime() >= cutoff;
    });
    scopeLabel.push(`last ${since_minutes}min`);
  }

  // If no scope provided, default to most recent trace (distinct traceId with latest timestamp)
  if (!trace_id && !since_minutes) {
    const traceTimestamps = new Map();
    for (const s of spans) {
      const t = spanStartTime(s);
      if (t && s.traceId) {
        const existing = traceTimestamps.get(s.traceId);
        if (!existing || t > existing) {
          traceTimestamps.set(s.traceId, t);
        }
      }
    }

    if (traceTimestamps.size > 1) {
      // Find the most recent trace
      let latestTrace = null;
      let latestTime = null;
      for (const [tid, time] of traceTimestamps) {
        if (!latestTime || time > latestTime) {
          latestTrace = tid;
          latestTime = time;
        }
      }
      if (latestTrace) {
        spans = spans.filter(s => s.traceId === latestTrace);
        scopeLabel.push(`latest trace (${spans.length}/${originalCount} spans)`);
      }
    } else {
      scopeLabel.push(`all spans`);
    }
  }

  lines.push(`**Source:** \`${tracePath}\` (${(fileSize / 1024).toFixed(1)} KB)`);
  lines.push(`**Scope:** ${scopeLabel.join(', ') || 'all'}`);
  lines.push(`**Spans analyzed:** ${spans.length} of ${originalCount} total`);
  lines.push('');

  if (spans.length === 0) {
    lines.push('⚠️ No spans match the specified scope. Try wider `since_minutes` or omit `trace_id`.');
    return lines.join('\n');
  }

  // ── Build span tree using parentSpanId ────────────────────────────
  const { roots, byId } = buildSpanTree(spans);

  // ── Categorize spans ─────────────────────────────────────────────
  const chatSpans = spans.filter(s => s.name && (s.name.startsWith('chat ') || extractModelFromSpan(s)));
  const agentSpans = spans.filter(s => s.name && (s.name.includes('invoke_agent') || s.name.includes('task')));
  const toolSpans = spans.filter(s => s.name && s.name.startsWith('execute_tool'));

  lines.push('## Span Summary');
  lines.push('');
  lines.push(`| Category | Count |`);
  lines.push(`|---|---|`);
  lines.push(`| 🤖 LLM Chat Spans | ${chatSpans.length} |`);
  lines.push(`| 🔧 Agent Invocation Spans | ${agentSpans.length} |`);
  lines.push(`| 🛠️ Tool Execution Spans | ${toolSpans.length} |`);
  lines.push(`| 📊 Total Spans | ${spans.length} |`);
  lines.push('');

  // ── Model usage matrix ───────────────────────────────────────────
  if (chatSpans.length > 0) {
    const modelStats = new Map();

    for (const span of chatSpans) {
      const model = extractModelFromSpan(span) || 'unknown';
      if (!modelStats.has(model)) {
        modelStats.set(model, { calls: 0, totalMs: 0, spans: [] });
      }
      const entry = modelStats.get(model);
      entry.calls++;
      entry.spans.push(span);
      const dur = spanDurationMs(span);
      if (dur !== null) entry.totalMs += dur;
    }

    lines.push('## Model Usage Verification');
    lines.push('');
    lines.push('| Model | Calls | Avg Latency | Provider Tier |');
    lines.push('|---|---|---|---|');

    const tierMap = {
      'claude-opus': 'Premium', 'claude-sonnet': 'Standard', 'claude-haiku': 'Fast',
      'gpt-5.4-mini': 'Fast', 'gpt-5-mini': 'Fast', 'gpt-4.1': 'Fast',
      'gpt-5': 'Standard', 'gpt-codex': 'Standard',
      'gemini-2.5-pro': 'Standard', 'gemini-2.5-flash': 'Fast',
    };

    for (const [model, stats] of [...modelStats.entries()].sort((a, b) => b[1].calls - a[1].calls)) {
      const avgMs = stats.totalMs > 0 ? `${Math.round(stats.totalMs / stats.calls)}ms` : 'N/A';
      const tier = Object.entries(tierMap).find(([k]) => model.includes(k))?.[1] || '—';
      lines.push(`| \`${model}\` | ${stats.calls} | ${avgMs} | ${tier} |`);
    }
    lines.push('');
  }

  // ── Per-agent model attribution (using span tree) ────────────────
  if (agentSpans.length > 0) {
    lines.push('## Per-Agent Model Attribution');
    lines.push('');
    lines.push('> Correlation method: `parentSpanId` → `spanId` tree traversal');
    lines.push('');

    for (const agentSpan of agentSpans) {
      const node = byId.get(agentSpan.spanId);
      if (!node) continue;

      // Walk the subtree to find all chat spans under this agent
      const childModels = [];
      const childTools = [];
      walkTree(node, (n) => {
        const model = extractModelFromSpan(n.span);
        if (model) {
          const dur = spanDurationMs(n.span);
          childModels.push({ model, durationMs: dur });
        }
        if (n.span.name && n.span.name.startsWith('execute_tool')) {
          childTools.push(n.span.name.replace('execute_tool ', ''));
        }
      });

      const uniqueModels = [...new Set(childModels.map(m => m.model))];
      const totalTurns = childModels.length;
      const toolList = [...new Set(childTools)];

      lines.push(`### 🤖 \`${agentSpan.name}\``);
      lines.push('');
      lines.push(`| Attribute | Value |`);
      lines.push(`|---|---|`);
      lines.push(`| **Model(s) Used** | ${uniqueModels.map(m => `\`${m}\``).join(', ') || '—'} |`);
      lines.push(`| **LLM Turns** | ${totalTurns} |`);
      lines.push(`| **Tools Invoked** | ${toolList.join(', ') || 'none'} |`);

      const dur = spanDurationMs(agentSpan);
      if (dur !== null) {
        lines.push(`| **Total Duration** | ${Math.round(dur)}ms (${(dur / 1000).toFixed(1)}s) |`);
      }

      lines.push('');
    }
  }

  // ── Behavioral fingerprint (corroborating evidence) ──────────────
  if (chatSpans.length > 0) {
    lines.push('## Behavioral Fingerprint');
    lines.push('');
    lines.push('> Latency patterns corroborate model identity — Premium models are slower than Fast tier.');
    lines.push('');

    const latencies = chatSpans
      .map(s => ({ model: extractModelFromSpan(s), ms: spanDurationMs(s) }))
      .filter(x => x.ms !== null);

    if (latencies.length > 0) {
      const buckets = new Map();
      for (const { model, ms } of latencies) {
        if (!buckets.has(model)) buckets.set(model, []);
        buckets.get(model).push(ms);
      }

      lines.push('| Model | Min | Avg | Max | Pattern |');
      lines.push('|---|---|---|---|---|');

      for (const [model, times] of [...buckets.entries()].sort((a, b) => avg(b[1]) - avg(a[1]))) {
        const mn = Math.round(Math.min(...times));
        const mx = Math.round(Math.max(...times));
        const av = Math.round(avg(times));
        const pattern = av > 10000 ? '🐢 Slow (Premium?)' :
                        av > 3000  ? '🚶 Medium (Standard?)' :
                                     '⚡ Fast (Haiku/Mini?)';
        lines.push(`| \`${model}\` | ${mn}ms | ${av}ms | ${mx}ms | ${pattern} |`);
      }
      lines.push('');
    }
  }

  // ── Tool usage breakdown ─────────────────────────────────────────
  if (toolSpans.length > 0) {
    lines.push('## Tool Usage');
    lines.push('');

    const toolCounts = new Map();
    for (const s of toolSpans) {
      const name = s.name.replace('execute_tool ', '');
      toolCounts.set(name, (toolCounts.get(name) || 0) + 1);
    }

    lines.push('| Tool | Calls |');
    lines.push('|---|---|');
    for (const [name, count] of [...toolCounts.entries()].sort((a, b) => b[1] - a[1])) {
      lines.push(`| \`${name}\` | ${count} |`);
    }
    lines.push('');
  }

  // ── Verification verdict ─────────────────────────────────────────
  lines.push('## Verification Verdict');
  lines.push('');

  const warnings = [];

  // Check for unknown models
  const unknownModels = chatSpans.filter(s => !extractModelFromSpan(s));
  if (unknownModels.length > 0) {
    warnings.push(`${unknownModels.length} span(s) have no identifiable model — attribution may be incomplete`);
  }

  // Check for content capture
  if (process.env[CONTENT_CAPTURE_VAR] === 'true') {
    warnings.push('🔴 Content capture is ON — trace file may contain sensitive data');
  }

  if (warnings.length > 0) {
    lines.push('**Warnings:**');
    for (const w of warnings) {
      lines.push(`- ⚠️ ${w}`);
    }
    lines.push('');
  }

  const chatCount = chatSpans.length;
  const modelCount = new Set(chatSpans.map(s => extractModelFromSpan(s)).filter(Boolean)).size;

  lines.push(`✅ **Verification complete** — ${chatCount} LLM calls across ${modelCount} model(s) verified via OTel trace data.`);

  return lines.join('\n');
}

function walkTree(node, visitor) {
  visitor(node);
  for (const child of node.children) {
    walkTree(child, visitor);
  }
}

function avg(arr) {
  return arr.reduce((a, b) => a + b, 0) / arr.length;
}

// ── Extension Registration ─────────────────────────────────────────────

const session = await joinSession({
  tools: [
    {
      name: 'otel_setup',
      description:
        'Generate portable shell commands to enable Copilot CLI OpenTelemetry tracing. ' +
        'Supports file (JSONL), Jaeger, and OTLP profiles. Cross-platform: PowerShell, cmd, bash, zsh, fish. ' +
        'Use this before restarting the CLI to enable model verification.',
      parameters: {
        type: 'object',
        properties: {
          profile: {
            type: 'string',
            enum: ['file', 'jaeger', 'otlp'],
            description: 'Export profile: "file" writes JSONL locally (simplest), "jaeger" exports to local Jaeger, "otlp" exports to a custom OTLP endpoint.',
            default: 'file',
          },
          trace_path: {
            type: 'string',
            description: 'Custom trace file path for "file" profile. Defaults to ~/.copilot/otel-traces.jsonl',
          },
          endpoint: {
            type: 'string',
            description: 'OTLP endpoint URL for "jaeger" or "otlp" profiles. Defaults to http://localhost:4318',
          },
          shell: {
            type: 'string',
            enum: ['powershell', 'cmd', 'bash', 'fish'],
            description: 'Target shell for the setup commands. Auto-detected if omitted.',
          },
        },
      },
      handler: async (params) => otelSetup(params || {}),
    },
    {
      name: 'otel_status',
      description:
        'Check current OpenTelemetry configuration status. Shows active env vars, export mode, ' +
        'trace file health, Docker daemon availability, and privacy alerts. ' +
        'Run this after restarting the CLI with OTel to confirm tracing is active.',
      parameters: { type: 'object', properties: {} },
      handler: async () => otelStatus(),
    },
    {
      name: 'otel_verify',
      description:
        'Analyze OTel trace file and produce a Model Verification Report. ' +
        'Proves which LLM model handled each sub-agent by building a span tree from parentSpanId linkage. ' +
        'Includes model usage matrix, per-agent attribution, behavioral fingerprinting, and tool usage. ' +
        'Auto-scopes to the most recent trace to avoid mixing sessions.',
      parameters: {
        type: 'object',
        properties: {
          trace_path: {
            type: 'string',
            description: 'Path to the JSONL trace file. Auto-detected from COPILOT_OTEL_FILE_EXPORTER_PATH env var if omitted.',
          },
          since_minutes: {
            type: 'number',
            description: 'Only analyze spans from the last N minutes. Prevents mixing old sessions.',
          },
          trace_id: {
            type: 'string',
            description: 'Filter to a specific OTel traceId. Use for surgical verification of one agent invocation.',
          },
        },
      },
      handler: async (params) => otelVerify(params || {}),
    },
  ],
});
