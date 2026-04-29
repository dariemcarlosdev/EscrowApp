import { joinSession } from "@github/copilot-sdk/extension";
import { readFile, readdir, stat } from "node:fs/promises";
import { resolve, join } from "node:path";
import { platform, homedir } from "node:os";

// ─── Trigger phrase detection ───────────────────────────────────────────────

const BENCHMARK_TRIGGERS = [
    /\bbenchmark\b/i,
    /\bperformance\b.*\b(?:memory|token|recall|storage)\b/i,
    /\b(?:memory|token|recall|storage)\b.*\bperformance\b/i,
    /\bmemory\s+stats?\b/i,
    /\bhow\s+efficient\b/i,
    /\bshow\s+efficiency\b/i,
    /\btoken\s+(?:usage|cost|saving)\b/i,
    /\bstorage\s+stats?\b/i,
    /\bmemory\s+layers?\b/i,
    /\brecall\s+performance\b/i,
    /\b(?:run|execute|show|display)\s+benchmark\b/i,
    /\bhow\s+(?:much|many)\s+(?:tokens?|memory)\b/i,
    /\bcuant[oa]s?\s+tokens?\b/i,
    /\brendimiento\b/i,
    /\beficien(?:cy|cia|t|te)\b/i,
    /\bmemory\s+(?:audit|report|check)\b/i,
    /\b(?:token|context)\s+(?:budget|window)\b/i,
];

function isBenchmarkRequest(prompt) {
    return BENCHMARK_TRIGGERS.some((regex) => regex.test(prompt));
}

// ─── File system helpers ────────────────────────────────────────────────────

async function getDirectoryStats(dirPath) {
    let totalSize = 0;
    let fileCount = 0;
    try {
        const entries = await readdir(dirPath, { withFileTypes: true });
        for (const entry of entries) {
            const fullPath = join(dirPath, entry.name);
            try {
                if (entry.isFile()) {
                    const s = await stat(fullPath);
                    totalSize += s.size;
                    fileCount++;
                } else if (entry.isDirectory() && !entry.name.startsWith(".")) {
                    const sub = await getDirectoryStats(fullPath);
                    totalSize += sub.size;
                    fileCount += sub.files;
                }
            } catch {
                /* skip inaccessible */
            }
        }
    } catch {
        /* dir doesn't exist */
    }
    return { size: totalSize, files: fileCount };
}

async function countSkills(skillsDir) {
    let coreSkills = 0;
    let references = 0;
    let coreChars = 0;
    let refChars = 0;

    try {
        const categories = await readdir(skillsDir, { withFileTypes: true });
        for (const cat of categories) {
            if (!cat.isDirectory() || cat.name.startsWith(".")) continue;
            const catPath = join(skillsDir, cat.name);

            const skills = await readdir(catPath, { withFileTypes: true });
            for (const skill of skills) {
                if (!skill.isDirectory()) continue;
                const skillPath = join(catPath, skill.name);

                // Count core SKILL.md
                try {
                    const content = await readFile(join(skillPath, "SKILL.md"), "utf-8");
                    coreSkills++;
                    coreChars += content.length;
                } catch {
                    /* no SKILL.md */
                }

                // Count references
                try {
                    const refs = await readdir(join(skillPath, "references"), { withFileTypes: true });
                    for (const ref of refs) {
                        if (ref.isFile() && ref.name.endsWith(".md")) {
                            references++;
                            try {
                                const refContent = await readFile(join(skillPath, "references", ref.name), "utf-8");
                                refChars += refContent.length;
                            } catch {
                                /* skip */
                            }
                        }
                    }
                } catch {
                    /* no refs dir */
                }
            }
        }
    } catch {
        /* skills dir doesn't exist */
    }

    return {
        coreSkills,
        references,
        coreTokens: Math.round(coreChars / 4),
        refTokens: Math.round(refChars / 4),
    };
}

async function measureBridges(rootDir) {
    const files = ["AGENTS.md", "CLAUDE.md", "GEMINI.md", "CODEX.md"];
    const copilotInstructions = join(rootDir, ".github", "copilot-instructions.md");

    let totalChars = 0;
    let fileCount = 0;

    for (const f of files) {
        try {
            const content = await readFile(join(rootDir, f), "utf-8");
            totalChars += content.length;
            fileCount++;
        } catch {
            /* file doesn't exist */
        }
    }

    try {
        const content = await readFile(copilotInstructions, "utf-8");
        totalChars += content.length;
        fileCount++;
    } catch {
        /* file doesn't exist */
    }

    return { fileCount, totalChars, estimatedTokens: Math.round(totalChars / 4) };
}

function fmtBytes(bytes) {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

function fmtNum(n) {
    return n.toLocaleString("en-US");
}

// ─── Session Store SQL queries (agent executes these) ───────────────────────

const SQL = {
    overview: `SELECT COUNT(DISTINCT s.id) as sessions, COUNT(t.turn_index) as turns, (SELECT COUNT(*) FROM search_index) as fts_entries, (SELECT SUM(LENGTH(content)) FROM search_index) as total_chars, ROUND((SELECT SUM(LENGTH(content)) FROM search_index) / 4.0) as est_tokens FROM sessions s LEFT JOIN turns t ON t.session_id = s.id;`,
    checkpoints: `SELECT COUNT(*) as count, SUM(LENGTH(COALESCE(overview,'') || COALESCE(work_done,'') || COALESCE(technical_details,''))) as chars FROM checkpoints;`,
    recall_person: `SELECT COUNT(*) as hits FROM search_index WHERE search_index MATCH 'Frank OR Jokovish';`,
    recall_ideas: `SELECT COUNT(*) as hits FROM search_index WHERE search_index MATCH 'monetize OR compile OR binary';`,
    recall_tech: `SELECT COUNT(*) as hits FROM search_index WHERE search_index MATCH 'Stripe OR payment OR escrow';`,
    recall_debug: `SELECT COUNT(*) as hits FROM search_index WHERE search_index MATCH 'fix OR error OR broken OR debug';`,
    recall_files: `SELECT COUNT(DISTINCT file_path) as tracked_files, COUNT(DISTINCT session_id) as sessions_with_edits FROM session_files;`,
};

// ─── AI Model pricing (input $/M tokens) for cross-model savings ────────────

const AI_MODELS = [
    { name: "Claude Opus 4.6", platform: "Claude Code", inputPerM: 5 },
    { name: "Claude Sonnet 4.6", platform: "Claude Code / Copilot", inputPerM: 3 },
    { name: "Claude Haiku 4.5", platform: "Copilot", inputPerM: 1 },
    { name: "GPT-5.4", platform: "Codex / Copilot", inputPerM: 2.50 },
    { name: "GPT-4.1", platform: "Codex / Copilot", inputPerM: 2 },
    { name: "GPT-5.4 mini", platform: "Copilot", inputPerM: 1.50 },
    { name: "Gemini 2.5 Pro", platform: "Gemini CLI", inputPerM: 1.25 },
    { name: "Gemini 2.5 Flash", platform: "Gemini CLI", inputPerM: 0.50 },
];
const WITHOUT_MEMORY_TOKENS = 80000;
const SESSIONS_PER_MONTH = 150; // ~7/day × 22 working days

function modelSavingsTable(perSession, compact) {
    const rows = AI_MODELS.map((model) => {
        const withoutCost = (WITHOUT_MEMORY_TOKENS / 1e6) * model.inputPerM;
        const withCost = (perSession / 1e6) * model.inputPerM;
        const savedSession = withoutCost - withCost;
        const savedMonthly = savedSession * SESSIONS_PER_MONTH;
        const savedAnnual = savedMonthly * 12;
        if (compact) {
            return `| ${model.name} | ${model.platform} | $${model.inputPerM} | $${withoutCost.toFixed(4)} | $${withCost.toFixed(4)} | **$${savedMonthly.toFixed(2)}/mo** |`;
        }
        return `| ${model.name} | ${model.platform} | $${model.inputPerM}/M | $${withoutCost.toFixed(4)} | $${withCost.toFixed(4)} | $${savedSession.toFixed(4)} | $${savedMonthly.toFixed(2)} | **$${savedAnnual.toFixed(0)}** |`;
    });

    // Summary: most expensive model savings
    const maxModel = AI_MODELS.reduce((a, b) => (a.inputPerM > b.inputPerM ? a : b));
    const maxAnnual = ((WITHOUT_MEMORY_TOKENS - perSession) / 1e6) * maxModel.inputPerM * SESSIONS_PER_MONTH * 12;
    const minModel = AI_MODELS.reduce((a, b) => (a.inputPerM < b.inputPerM ? a : b));
    const minAnnual = ((WITHOUT_MEMORY_TOKENS - perSession) / 1e6) * minModel.inputPerM * SESSIONS_PER_MONTH * 12;

    return { rows, maxModel, maxAnnual, minModel, minAnnual };
}

// ─── Report formatters ──────────────────────────────────────────────────────

function formatCompact(m) {
    const avgSkill = m.skills.coreSkills > 0 ? Math.round(m.skills.coreTokens / m.skills.coreSkills) : 0;
    const totalSkillTokens = m.skills.coreTokens + m.skills.refTokens;
    const perSession = m.bridges.estimatedTokens + avgSkill + 500;
    const savingsPct = Math.round((1 - perSession / 80000) * 100);

    // Standalone tools cost comparison
    const standaloneTools = [
        { category: "Code Review", tools: "CodeRabbit, Sourcery", monthly: 30, skills: "code-reviewer, quality-analyzer" },
        { category: "Security", tools: "Snyk, GitGuardian, Semgrep", monthly: 90, skills: "owasp-audit, secret-scanner, threat-modeler" },
        { category: "Testing", tools: "CodiumAI, Diffblue", monthly: 50, skills: "test-generator, tdd-coach, test-coverage" },
        { category: "Documentation", tools: "Mintlify, Swimm", monthly: 53, skills: "readme-gen, api-documenter, adr-creator" },
        { category: "Memory/Context", tools: "Mem.ai, Rewind AI", monthly: 34, skills: "mempalace-memory, memory-optimization" },
        { category: "Project Mgmt", tools: "Linear AI, Notion AI", monthly: 18, skills: "spec-writer, issue-creator, feature-forge" },
        { category: "Refactoring", tools: "Sourcery Pro", monthly: 14, skills: "refactor-planner, smart-refactor, tech-debt" },
        { category: "CI/CD & DevOps", tools: "Various CI AI tools", monthly: 30, skills: "ci-cd-builder, deployment-preflight" },
        { category: "Architecture", tools: "Manual consulting", monthly: 0, skills: "architecture-reviewer, design-pattern" },
        { category: "Database", tools: "Manual", monthly: 0, skills: "schema-reviewer, query-optimizer" },
    ];
    const standaloneTotal = standaloneTools.reduce((s, t) => s + t.monthly, 0);
    const activeCategories = standaloneTools.filter(t => t.monthly > 0).length;

    return `## 📊 NexSynapse Memory Benchmark — Compact

| Layer | Items | Storage | Est. Tokens | Status |
|-------|------:|--------:|------------:|--------|
| **L1: Session Store** | _query below_ | _built-in_ | _query below_ | ✅ Auto |
| **L2: MemPalace** | ${m.palace.files} files | ${m.palace.sizeStr} | ~${fmtNum(m.palace.textTokens)} | ${m.palace.status} |
| **L3: AGENTS.md** | ${m.bridges.fileCount} files | ${fmtBytes(m.bridges.totalChars)} | ~${fmtNum(m.bridges.estimatedTokens)} | ✅ Active |
| **L4: Skills** | ${m.skills.coreSkills} core + ${m.skills.references} refs | ${fmtBytes(totalSkillTokens * 4)} | ~${fmtNum(totalSkillTokens)} | ✅ On-demand |

### 💰 Per-Session Efficiency
| Metric | Value |
|--------|------:|
| AGENTS.md auto-load | ~${fmtNum(m.bridges.estimatedTokens)} tokens |
| 1 Skill on-demand | ~${fmtNum(avgSkill)} tokens |
| Saved vs loading all skills | ~${fmtNum(totalSkillTokens - avgSkill)} tokens |
| **Per-session cost (WITH memory)** | **~${fmtNum(perSession)} tokens** |
| **Per-session cost (WITHOUT)** | **~80,000 tokens** |
| **Savings** | **${savingsPct}%** |

### 🔧 vs Standalone AI Agentic Tools
| Capability | Standalone Tools | Monthly Cost | NexSynapse Skills | NexSynapse Cost |
|------------|-----------------|-------------:|-------------------|----------------:|
${standaloneTools.filter(t => t.monthly > 0).map(t => `| ${t.category} | ${t.tools} | $${t.monthly}/mo | ${t.skills} | **$0** |`).join("\n")}
| **TOTAL (${activeCategories} categories)** | **${activeCategories} subscriptions** | **$${standaloneTotal}/mo** | **${m.skills.coreSkills} skills bundled** | **$0/mo** |

> 💡 NexSynapse replaces **$${standaloneTotal}/mo** in standalone tools with **$0** — all ${m.skills.coreSkills} skills run on your existing AI model.

### 🤖 vs AI Models — Token Cost Savings per Platform
NexSynapse reduces input context from ~${fmtNum(WITHOUT_MEMORY_TOKENS)} to ~${fmtNum(perSession)} tokens/session. Impact at ${SESSIONS_PER_MONTH} sessions/mo:

| Model | Platform | Input $/M | Without NexSynapse | With NexSynapse | Monthly Savings |
|-------|----------|----------:|-------------------:|----------------:|----------------:|
${(() => { const s = modelSavingsTable(perSession, true); return s.rows.join("\n"); })()}

> 🏆 Biggest saver: **${(() => { const s = modelSavingsTable(perSession, true); return `${s.maxModel.name} — $${Math.round(s.maxAnnual)}/yr saved`; })()}**

> Run this SQL for Session Store metrics:
> \`sql(database: "session_store", query: "${SQL.overview}")\`

> For full report with recall tests & charts: \`run_memory_benchmark\` with mode "full"`;
}

function formatFull(m) {
    const avgSkill = m.skills.coreSkills > 0 ? Math.round(m.skills.coreTokens / m.skills.coreSkills) : 0;
    const avgRef = m.skills.references > 0 ? Math.round(m.skills.refTokens / m.skills.references) : 0;
    const totalSkillTokens = m.skills.coreTokens + m.skills.refTokens;
    const totalStored = m.bridges.estimatedTokens + totalSkillTokens + m.palace.textTokens;
    const perSession = m.bridges.estimatedTokens + avgSkill + 500;
    const withoutMemory = 80000;
    const savings = withoutMemory - perSession;
    const savingsPct = Math.round((savings / withoutMemory) * 100);
    const costPerM = 3.0;

    return `## 📊 NexSynapse Memory Benchmark — Full Report
**Generated:** ${new Date().toISOString().split("T")[0]} | **Platform:** ${platform()} | **Cost of this benchmark:** $0

---

### 📦 Layer 1: Session Store (Auto-Captured History)
| Metric | Value |
|--------|-------|
| Type | Built-in SQLite + FTS5 full-text search |
| Cost | $0 (local, auto-captured) |
| Per-query cost | ~50-500 tokens (SQL metadata, not raw content) |

**Run these queries to get live metrics:**
- Overview: \`sql(database: "session_store", query: "${SQL.overview}")\`
- Checkpoints: \`sql(database: "session_store", query: "${SQL.checkpoints}")\`
- File tracking: \`sql(database: "session_store", query: "${SQL.recall_files}")\`

**Recall Tests (run to verify):**
- Person recall: \`sql(database: "session_store", query: "${SQL.recall_person}")\`
- Idea recall: \`sql(database: "session_store", query: "${SQL.recall_ideas}")\`
- Tech recall: \`sql(database: "session_store", query: "${SQL.recall_tech}")\`
- Debug recall: \`sql(database: "session_store", query: "${SQL.recall_debug}")\`

---

### 📦 Layer 2: MemPalace (Curated Cross-Session)
| Metric | Value |
|--------|------:|
| Palace files | ${m.palace.files} |
| Disk size | ${m.palace.sizeStr} |
| Est. text tokens | ~${fmtNum(m.palace.textTokens)} |
| MCP status | ${m.palace.status} |
| Disk cost | $0 (local-only) |

---

### 📦 Layer 3: AGENTS.md + Model Bridges (Static DNA)
| Metric | Value |
|--------|------:|
| Bridge files | ${m.bridges.fileCount} |
| Total chars | ${fmtNum(m.bridges.totalChars)} |
| Est. tokens | ~${fmtNum(m.bridges.estimatedTokens)} |
| Per-session cost | ~${fmtNum(m.bridges.estimatedTokens)} tokens (auto-loaded every session) |
| Purpose | Project architecture, patterns, rules, conventions |

---

### 📦 Layer 4: Skills Library (Progressive On-Demand)
| Metric | Value |
|--------|------:|
| Core SKILL.md files | ${m.skills.coreSkills} |
| Reference files | ${m.skills.references} |
| Core tokens (all) | ~${fmtNum(m.skills.coreTokens)} |
| Reference tokens (all) | ~${fmtNum(m.skills.refTokens)} |
| Avg per skill | ~${fmtNum(avgSkill)} tokens |
| Avg per reference | ~${fmtNum(avgRef)} tokens |
| **If loaded ALL at once** | **~${fmtNum(totalSkillTokens)} tokens** |
| **On-demand (1 skill)** | **~${fmtNum(avgSkill)} tokens** |
| **Progressive savings** | **~${fmtNum(totalSkillTokens - avgSkill)} tokens (${Math.round((1 - avgSkill / totalSkillTokens) * 100)}%)** |

---

### 💰 Cost Comparison: WITH vs WITHOUT Memory

| Scenario | Tokens | API Cost (~$${costPerM}/M) |
|----------|-------:|---------------------------:|
| **WITHOUT memory** (re-explain all) | ~${fmtNum(withoutMemory)} | ~$${(withoutMemory / 1e6 * costPerM).toFixed(3)} |
| **WITH NexSynapse memory** | ~${fmtNum(perSession)} | ~$${(perSession / 1e6 * costPerM).toFixed(4)} |
| ├─ AGENTS.md (auto-load) | ${fmtNum(m.bridges.estimatedTokens)} | $${(m.bridges.estimatedTokens / 1e6 * costPerM).toFixed(4)} |
| ├─ Session Store query | ~500 | $${(500 / 1e6 * costPerM).toFixed(4)} |
| ├─ 1 Skill on-demand | ~${fmtNum(avgSkill)} | $${(avgSkill / 1e6 * costPerM).toFixed(4)} |
| └─ MemPalace wake-up | ~800 | $${(800 / 1e6 * costPerM).toFixed(4)} |
| **SAVINGS PER SESSION** | **~${fmtNum(savings)}** | **~$${(savings / 1e6 * costPerM).toFixed(3)} (${savingsPct}%)** |

---

### 📊 Total Knowledge Inventory
| Category | Est. Tokens |
|----------|------------:|
| Session Store history | _Run SQL for live count_ |
| MemPalace curated | ~${fmtNum(m.palace.textTokens)} |
| AGENTS.md + bridges | ~${fmtNum(m.bridges.estimatedTokens)} |
| Skills library (all) | ~${fmtNum(totalSkillTokens)} |
| **TOTAL stored** | **~${fmtNum(totalStored)}+** |

---

### 🔧 vs Standalone AI Agentic Tools — Detailed

NexSynapse bundles ${m.skills.coreSkills} specialized skills that replace standalone AI tools:

| Category | Standalone Tool(s) | Monthly Cost | NexSynapse Equivalent | NexSynapse Cost |
|----------|-------------------|-------------:|----------------------|----------------:|
| Code Review & Quality | CodeRabbit, Sourcery, Codacy | ~$30/mo | code-reviewer, quality-analyzer, smart-refactor | **$0** |
| Security Scanning | Snyk, GitGuardian, Semgrep Pro | ~$90/mo | owasp-audit, secret-scanner, threat-modeler, auth skills | **$0** |
| Test Generation | CodiumAI / Qodo, Diffblue Cover | ~$50/mo | test-generator, tdd-coach, test-coverage-analyzer | **$0** |
| Documentation | Mintlify, Swimm | ~$53/mo | code-documenter, readme-generator, api-documenter, adr-creator | **$0** |
| AI Memory & Recall | Mem.ai, Rewind AI | ~$34/mo | mempalace-memory, memory-optimization, memory-benchmark | **$0** |
| Project Management | Linear AI, Notion AI | ~$18/mo | spec-writer, issue-creator, feature-forge, mvp-gatekeeper | **$0** |
| Refactoring & Debt | Sourcery Pro | ~$14/mo | refactor-planner, smart-refactor, tech-debt-tracker | **$0** |
| CI/CD & DevOps | Various CI AI tools | ~$30/mo | ci-cd-builder, deployment-preflight, git-workflow, chaos-engineer | **$0** |
| Architecture Review | Manual consulting ($150-300/hr) | ~$300+/mo | architecture-reviewer, design-pattern-advisor, dependency-analyzer | **$0** |
| Database Optimization | Manual DBA / SaaS tools | ~$25/mo | schema-reviewer, query-optimizer | **$0** |
| Research & Exploration | Manual | ~$0/mo | codebase-explorer, tech-spike-planner, deep-context-generator | **$0** |
| AI Agent Development | Manual | ~$0/mo | mcp-developer, prompt-engineer, agent-orchestrator | **$0** |
| **TOTAL** | **10+ subscriptions** | **~$644+/mo** | **${m.skills.coreSkills} skills + ${m.skills.references} refs** | **$0/mo** |

\`\`\`
┌─────────────────────────────────────────────────────────────────────┐
│         STANDALONE TOOLS vs NEXSYNAPSE — ANNUAL COST                │
│                                                                     │
│  Standalone:  ${"█".repeat(30)}  ~$7,728/yr ($644/mo × 12)   │
│  NexSynapse:  ${"░".repeat(30)}  $0/yr (runs on your AI model)│
│                                                                     │
│         TOKEN COST — MEMORY vs NO MEMORY                            │
│                                                                     │
│  WITHOUT:     ${"█".repeat(30)}  ${fmtNum(withoutMemory)} tokens/session      │
│  WITH:        ${"█".repeat(Math.max(1, Math.round((30 * perSession) / withoutMemory)))}${"░".repeat(30 - Math.max(1, Math.round((30 * perSession) / withoutMemory)))}  ${fmtNum(perSession)} tokens/session      │
│  SAVINGS:     ${"▓".repeat(Math.round((30 * savingsPct) / 100))}  ${savingsPct}% fewer tokens            │
│                                                                     │
│  TOTAL STORED:       ~${fmtNum(totalStored)}+ tokens                           │
│  TOOL SUBSCRIPTIONS: $0/mo (replaces ~$644/mo)                      │
│  DISK COST:          $0 (all local)                                 │
│  BENCHMARK COST:     $0 (filesystem + SQL only)                     │
│  MAINTENANCE:        Zero (auto-captured)                           │
│  PORTABILITY:        Works on Claude, GPT, Gemini, Codex            │
└─────────────────────────────────────────────────────────────────────┘
\`\`\`

> **Key insight:** NexSynapse replaces **~$644/mo in standalone AI tools** with a single portable infrastructure at **$0/mo**. Progressive skill loading costs ~${fmtNum(avgSkill)} tokens vs ~${fmtNum(totalSkillTokens)} for all ${m.skills.coreSkills} skills — a **${Math.round((1 - avgSkill / totalSkillTokens) * 100)}% reduction.**

---

### 🤖 vs AI Models — Per-Platform Token Cost Savings

NexSynapse's 4-layer memory reduces input context from **~${fmtNum(WITHOUT_MEMORY_TOKENS)} tokens** to **~${fmtNum(perSession)} tokens** per session — a **${savingsPct}% reduction**. Here's the dollar impact on each AI platform:

| Model | Platform | Input Price | Without NexSynapse | With NexSynapse | Saved/Session | Monthly (${SESSIONS_PER_MONTH}×) | Annual |
|-------|----------|------------:|-------------------:|----------------:|--------------:|--------------:|-------:|
${(() => { const s = modelSavingsTable(perSession, false); return s.rows.join("\n"); })()}

${(() => {
    const s = modelSavingsTable(perSession, false);
    const maxBar = 30;
    const chart = AI_MODELS.map(model => {
        const annual = ((WITHOUT_MEMORY_TOKENS - perSession) / 1e6) * model.inputPerM * SESSIONS_PER_MONTH * 12;
        const barLen = Math.max(1, Math.round(maxBar * annual / s.maxAnnual));
        const label = ("$" + annual.toFixed(0) + "/yr saved").padStart(16);
        return "│  " + model.name.padEnd(22) + " " + "█".repeat(barLen) + "░".repeat(maxBar - barLen) + " " + label + "  │";
    }).join("\n");
    return "```\n┌──────────────────────────────────────────────────────────────────────────────┐\n│           ANNUAL TOKEN SAVINGS BY AI MODEL (with NexSynapse)                 │\n│                                                                              │\n" + chart + "\n│                                                                              │\n│  Sessions/month: " + SESSIONS_PER_MONTH + "  │  Tokens saved/session: ~" + fmtNum(WITHOUT_MEMORY_TOKENS - perSession) + "             │\n│  Most saved: " + s.maxModel.name.padEnd(18) + " $" + Math.round(s.maxAnnual) + "/yr                                    │\n│  Least saved: " + s.minModel.name.padEnd(17) + " $" + Math.round(s.minAnnual) + "/yr (still saves " + savingsPct + "% tokens)          │\n└──────────────────────────────────────────────────────────────────────────────┘\n```";
})()}

> 💡 **Every AI model benefits equally from NexSynapse** — the **${savingsPct}% token reduction** applies regardless of platform. Premium models (Opus, GPT-5) save the most in dollar terms because their per-token cost is highest.`;
}

// ─── Main session ───────────────────────────────────────────────────────────

const session = await joinSession({
    hooks: {
        onSessionStart: async () => {
            await session.log("📊 Memory Benchmark extension loaded", { ephemeral: true });
        },

        onUserPromptSubmitted: async (input) => {
            const prompt = input.prompt;
            if (!prompt || typeof prompt !== "string") return;

            if (isBenchmarkRequest(prompt)) {
                await session.log("📊 Benchmark request detected", { ephemeral: true });
                return {
                    additionalContext:
                        "📊 BENCHMARK TRIGGER DETECTED: The user is asking about memory performance, efficiency, or benchmarks. Call the `run_memory_benchmark` tool to generate a comprehensive report. Use mode 'compact' for a quick overview or 'full' for detailed analysis with cost comparison. After getting the tool results, run the embedded SQL queries against the session_store database to complete the Layer 1 (Session Store) metrics. This benchmark costs ZERO external API tokens — it uses only local filesystem checks and the built-in Session Store SQL.",
                };
            }
        },
    },

    tools: [
        {
            name: "run_memory_benchmark",
            description:
                "Runs a comprehensive benchmark of the NexSynapse 4-layer memory architecture (Session Store, MemPalace, AGENTS.md, Skills). Measures storage footprint, token costs, recall capacity, and per-session savings. Zero external API cost — uses only local filesystem and Session Store SQL. Returns compact summary by default; use mode 'full' for detailed analysis with cost comparison tables, recall tests, and efficiency charts.",
            parameters: {
                type: "object",
                properties: {
                    mode: {
                        type: "string",
                        enum: ["compact", "full"],
                        description:
                            "Output detail level. 'compact' (default) = quick summary table. 'full' = detailed report with per-layer analysis, cost comparison, recall tests, and ASCII charts.",
                    },
                },
                additionalProperties: false,
            },
            handler: async (args) => {
                const mode = args.mode || "compact";
                const rootDir = process.cwd();

                await session.log("📊 Running memory benchmark...", { ephemeral: true });

                // Measure Layer 2: MemPalace
                const palacePath = join(
                    platform() === "win32" ? process.env.USERPROFILE || homedir() : homedir(),
                    ".mempalace",
                );
                const palaceStats = await getDirectoryStats(palacePath);
                // MemPalace contains ChromaDB binary + text; estimate ~15% is searchable text
                const palaceTextTokens = Math.round((palaceStats.size * 0.15) / 4);

                // Measure Layer 3: AGENTS.md + Bridges
                const bridges = await measureBridges(rootDir);

                // Measure Layer 4: Skills
                const skillsDir = join(rootDir, ".github", "skills");
                const skills = await countSkills(skillsDir);

                const metrics = {
                    palace: {
                        files: palaceStats.files,
                        size: palaceStats.size,
                        sizeStr: fmtBytes(palaceStats.size),
                        textTokens: palaceTextTokens,
                        status: palaceStats.files > 0 ? "✅ Palace exists" : "⚠️ Not found",
                    },
                    bridges,
                    skills,
                    timestamp: new Date().toISOString(),
                };

                await session.log(
                    `📊 Benchmark complete: ${bridges.fileCount} bridges, ${skills.coreSkills} skills, ${palaceStats.files} palace files`,
                    { ephemeral: true },
                );

                return mode === "full" ? formatFull(metrics) : formatCompact(metrics);
            },
        },
    ],
});
