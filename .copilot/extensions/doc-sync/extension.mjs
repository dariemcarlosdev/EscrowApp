import { joinSession } from "@github/copilot-sdk/extension";
import { existsSync, statSync, readdirSync, readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

// ---------------------------------------------------------------------------
// Default configuration — used when doc-sync.config.json is missing or invalid.
// This is the single source of truth for fallback values.
// ---------------------------------------------------------------------------
const DEFAULT_CONFIG = {
  appRoot: "EscrowApp",
  projectFile: "EscrowApp.csproj",
  docsDir: "docs",
  planningDocs: [
    "docs/planning/implementation-plan.md",
    "docs/planning/task-checklist.md",
  ],
  featureMap: [
    { pattern: "Features/Escrow/HoldFunds", doc: "features/hold-funds" },
    { pattern: "Features/Escrow/CreateAndHoldFunds", doc: "features/hold-funds" },
    { pattern: "Features/Escrow/ReleaseFunds", doc: "features/release-funds" },
    { pattern: "Features/Escrow/DisputeFunds", doc: "features/dispute-funds" },
    { pattern: "Features/Escrow/CancelFunds", doc: "features/cancel-funds" },
    { pattern: "Services/Strategies", doc: "architecture/payment-strategies" },
    { pattern: "Services/", doc: "architecture/payment-strategies" },
    { pattern: "Infrastructure/Auth", doc: "cross-cutting/hybrid-identity" },
    { pattern: "Events/", doc: "architecture/event-bus" },
    { pattern: "Resources/", doc: "cross-cutting/localization" },
    { pattern: "Components/Pages/", doc: "features/landing-page" },
    { pattern: "Features/Escrow/Api", doc: "architecture/api-integration" },
    { pattern: "Features/Escrow/GetTransaction", doc: "architecture/api-integration" },
    { pattern: "Features/Escrow/ListTransactions", doc: "architecture/api-integration" },
    { pattern: "Infrastructure/Middleware", doc: "architecture/api-integration" },
    { pattern: "Infrastructure/Webhooks", doc: "architecture/stripe-webhooks" },
  ],
  watchedDirs: ["Features", "Services", "Models", "Events", "Components", "Infrastructure", "Resources"],
  planningAffectingDirs: ["Features", "Services", "Models", "Events", "Components", "Infrastructure", "Resources", "Data", "Migrations"],
  planningAffectingFiles: ["Program.cs"],
  planningAffectingPatterns: ["Tests/"],
  statusMap: [
    { label: "Escrow Hold Funds", srcDir: "Features/Escrow/HoldFunds", doc: "features/hold-funds" },
    { label: "Escrow Release Funds", srcDir: "Features/Escrow/ReleaseFunds", doc: "features/release-funds" },
    { label: "Escrow Dispute Funds", srcDir: "Features/Escrow/DisputeFunds", doc: "features/dispute-funds" },
    { label: "Cancel Funds", srcDir: "Features/Escrow/CancelFunds", doc: "features/cancel-funds" },
    { label: "Payment Strategies", srcDir: "Services/Strategies", doc: "architecture/payment-strategies" },
    { label: "Hybrid Identity", srcDir: "Infrastructure/Auth", doc: "cross-cutting/hybrid-identity" },
    { label: "Event Bus", srcDir: "Events", doc: "architecture/event-bus" },
    { label: "Localization", srcDir: "Resources", doc: "cross-cutting/localization" },
    { label: "Landing Page UI", srcDir: "Components/Pages", doc: "features/landing-page" },
    { label: "API Integration", srcDir: "Features/Escrow/Api", doc: "architecture/api-integration" },
    { label: "Stripe Webhooks", srcDir: "Infrastructure/Webhooks", doc: "architecture/stripe-webhooks" },
    { label: "Architecture", srcDir: "Models", doc: "architecture/overview" },
    { label: "Testing", srcDir: "../EscrowApp.Tests", doc: "cross-cutting/testing" },
  ],
  planningCheckDirs: [
    { label: "Features/Escrow", dir: "Features/Escrow" },
    { label: "Models", dir: "Models" },
    { label: "Events", dir: "Events" },
    { label: "Services", dir: "Services" },
    { label: "Data", dir: "Data" },
    { label: "Components/Pages", dir: "Components/Pages" },
    { label: "Infrastructure", dir: "Infrastructure" },
    { label: "Tests", dir: "../EscrowApp.Tests" },
  ],
  cooldowns: {
    featureDocReminderMs: 60_000,
    planningDocReminderMs: 120_000,
  },
  defaultDocFallback: "architecture/overview",
};

// ---------------------------------------------------------------------------
// Config loading — reads doc-sync.config.json relative to this extension file.
// Falls back to DEFAULT_CONFIG on any error.
// ---------------------------------------------------------------------------
let _configWarning = "";

/** Escape all regex metacharacters in a literal string. */
function escapeRegex(str) {
  return str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Convert a path pattern string (e.g. "Features/Escrow/HoldFunds") into a
 * RegExp that matches both forward-slash and backslash separators.
 * All regex metacharacters are escaped first, then literal `/` is replaced
 * with the cross-platform separator group `[/\\]`.
 */
function patternToRegex(pattern) {
  const escaped = escapeRegex(pattern);
  const crossPlatform = escaped.replace(/\\\//g, "[/\\\\]");
  return new RegExp(crossPlatform, "i");
}

/**
 * Build a single regex that matches any path containing one of the given
 * directory names as a path segment: `[/\\](DirA|DirB|...)[/\\]`
 */
function buildDirRegex(dirs) {
  const escaped = dirs.map(escapeRegex).join("|");
  return new RegExp(`[/\\\\](${escaped})[/\\\\]`, "i");
}

/**
 * Build the combined planning-affecting regex from dirs, files, and patterns.
 * Produces a regex like:
 *   /[/\\](Dir1|Dir2)[/\\]|[/\\](File1|File2)$|Pattern1|Pattern2/i
 */
function buildPlanningRegex(config) {
  const parts = [];

  // Directory segments
  if (config.planningAffectingDirs?.length) {
    const escaped = config.planningAffectingDirs.map(escapeRegex).join("|");
    parts.push(`[/\\\\](${escaped})[/\\\\]`);
  }

  // Exact filename matches at end of path
  if (config.planningAffectingFiles?.length) {
    const escaped = config.planningAffectingFiles.map(escapeRegex).join("|");
    parts.push(`[/\\\\](${escaped})$`);
  }

  // Freeform patterns (converted to cross-platform regex fragments)
  if (config.planningAffectingPatterns?.length) {
    for (const p of config.planningAffectingPatterns) {
      const escaped = escapeRegex(p);
      const crossPlatform = escaped.replace(/\\\//g, "[/\\\\]");
      parts.push(crossPlatform);
    }
  }

  return parts.length > 0
    ? new RegExp(parts.join("|"), "i")
    : /(?!)/; // never-matching fallback
}

/** Safely load and parse config; returns raw config or DEFAULT_CONFIG. */
function safeLoadConfig() {
  try {
    const extensionDir = dirname(fileURLToPath(import.meta.url));
    const configPath = join(extensionDir, "doc-sync.config.json");

    if (!existsSync(configPath)) {
      _configWarning = `Config file not found at ${configPath} — using defaults.`;
      return DEFAULT_CONFIG;
    }

    const raw = readFileSync(configPath, "utf-8");
    const parsed = JSON.parse(raw);

    // Basic validation: required fields
    if (!parsed.appRoot || !parsed.projectFile) {
      _configWarning = "Config missing required fields (appRoot, projectFile) — using defaults.";
      return DEFAULT_CONFIG;
    }

    return parsed;
  } catch (err) {
    _configWarning = `Failed to load doc-sync config: ${err.message} — using defaults.`;
    return DEFAULT_CONFIG;
  }
}

/**
 * Compile raw config data into the runtime structures used by the extension.
 * Keeps algorithmic behavior in code; config provides only data.
 */
function compileConfig(raw) {
  const featureMap = (raw.featureMap || []).map((entry) => ({
    pattern: patternToRegex(entry.pattern),
    doc: entry.doc,
  }));

  const watchedDirsRegex = buildDirRegex(raw.watchedDirs || []);
  const planningPathsRegex = buildPlanningRegex(raw);
  const statusMap = raw.statusMap || [];
  const planningCheckDirs = raw.planningCheckDirs || [];
  const planningDocs = raw.planningDocs || [];
  const cooldowns = { ...DEFAULT_CONFIG.cooldowns, ...(raw.cooldowns || {}) };

  return {
    appRoot: raw.appRoot || DEFAULT_CONFIG.appRoot,
    projectFile: raw.projectFile || DEFAULT_CONFIG.projectFile,
    docsDir: raw.docsDir || DEFAULT_CONFIG.docsDir,
    defaultDocFallback: raw.defaultDocFallback || DEFAULT_CONFIG.defaultDocFallback,
    featureMap,
    watchedDirsRegex,
    planningPathsRegex,
    planningDocs,
    statusMap,
    planningCheckDirs,
    cooldowns,
  };
}

// Load and compile config at startup (safe — never throws)
const CONFIG = compileConfig(safeLoadConfig());

// ---------------------------------------------------------------------------
// Runtime state
// ---------------------------------------------------------------------------
const lastReminder = new Map();
let lastPlanningReminder = 0;
let planningDocsRecentlyEdited = false;

// ---------------------------------------------------------------------------
// Helper functions
// ---------------------------------------------------------------------------
function findAppRoot(cwd) {
  const candidates = [
    join(cwd, CONFIG.appRoot),
    cwd,
  ];
  for (const candidate of candidates) {
    if (
      existsSync(join(candidate, CONFIG.docsDir)) &&
      existsSync(join(candidate, CONFIG.projectFile))
    ) {
      return candidate;
    }
  }
  // Fallback: check if docsDir exists at cwd/appRoot
  if (existsSync(join(cwd, CONFIG.appRoot, CONFIG.docsDir))) {
    return join(cwd, CONFIG.appRoot);
  }
  return undefined;
}

function mapFileToDoc(filePath) {
  const normalized = filePath.replace(/\\/g, "/");
  for (const entry of CONFIG.featureMap) {
    if (entry.pattern.test(normalized)) {
      return entry.doc;
    }
  }
  return CONFIG.defaultDocFallback;
}

function getLatestMtime(dirPath) {
  let latest = 0;
  if (!existsSync(dirPath)) return latest;

  try {
    const entries = readdirSync(dirPath, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = join(dirPath, entry.name);
      try {
        if (entry.isDirectory()) {
          const childMtime = getLatestMtime(fullPath);
          if (childMtime > latest) latest = childMtime;
        } else if (entry.isFile()) {
          const mtime = statSync(fullPath).mtimeMs;
          if (mtime > latest) latest = mtime;
        }
      } catch {
        // Skip inaccessible entries
      }
    }
  } catch {
    // Skip inaccessible directories
  }
  return latest;
}

function formatTimestamp(ms) {
  if (ms === 0) return "N/A";
  return new Date(ms).toISOString().replace("T", " ").substring(0, 19);
}

const session = await joinSession({
  hooks: {
    onSessionStart: async () => {
      if (_configWarning) {
        await session.log(`[doc-sync] WARNING: ${_configWarning}`);
      }
      await session.log("Doc-Sync extension loaded (config-driven)");
    },

    onPostToolUse: async (input) => {
      if (input.toolName !== "edit" && input.toolName !== "create") {
        return undefined;
      }

      const filePath = typeof input.toolArgs?.path === "string"
        ? input.toolArgs.path
        : undefined;
      if (!filePath) return undefined;

      const contextParts = [];

      // --- Feature docs reminder ---
      if (CONFIG.watchedDirsRegex.test(filePath)) {
        const docFolder = mapFileToDoc(filePath);
        const now = Date.now();
        const lastTime = lastReminder.get(docFolder) || 0;
        if (now - lastTime >= CONFIG.cooldowns.featureDocReminderMs) {
          lastReminder.set(docFolder, now);
          contextParts.push(
            `DOCS SYNC REQUIRED: You modified code related to "${docFolder}".`,
            `Per project rules, the corresponding doc in ${CONFIG.docsDir}/${docFolder}/ must be updated to reflect these changes.`,
          );
        }
      }

      // --- Planning docs reminder ---
      // Check if user just edited a planning doc — suppress further reminders
      const isPlanningDocEdit = CONFIG.planningDocs.some((pd) =>
        filePath.replace(/\\/g, "/").includes(pd),
      );
      if (isPlanningDocEdit) {
        planningDocsRecentlyEdited = true;
        lastPlanningReminder = Date.now();
        // No reminder needed — they're already editing planning docs
        return contextParts.length > 0
          ? { additionalContext: contextParts.join(" ") }
          : undefined;
      }

      // Only remind about planning docs for status-affecting edits
      if (CONFIG.planningPathsRegex.test(filePath)) {
        const now = Date.now();
        const cooldownElapsed = now - lastPlanningReminder >= CONFIG.cooldowns.planningDocReminderMs;

        if (cooldownElapsed && !planningDocsRecentlyEdited) {
          lastPlanningReminder = now;
          contextParts.push(
            "PLANNING DOCS UPDATE REQUIRED: You modified code that affects project status.",
            "Update `docs/planning/implementation-plan.md` (phase status, completion %) and",
            "`docs/planning/task-checklist.md` (check/uncheck items, add new tasks) to reflect current state.",
            "Update the 'Last synced with codebase' date in both files.",
          );
        }

        // Reset the suppression flag after cooldown — forces a reminder
        // if planning docs haven't been touched since the last code edit burst
        if (cooldownElapsed && planningDocsRecentlyEdited) {
          planningDocsRecentlyEdited = false;
        }
      }

      return contextParts.length > 0
        ? { additionalContext: contextParts.join(" ") }
        : undefined;
    },
  },

  tools: [
    {
      name: "docs_status",
      description:
        "Compares last-modified timestamps of source code directories vs their corresponding docs/ markdown files. Reports which docs are potentially stale.",
      parameters: {
        type: "object",
        properties: {},
      },
      handler: async () => {
        const cwd = process.cwd();
        const appRoot = findAppRoot(cwd);

        if (!appRoot) {
          return `Could not locate ${CONFIG.appRoot} directory. Searched from: ${cwd}`;
        }

        const docsRoot = join(appRoot, CONFIG.docsDir);
        const lines = [
          "# Documentation Freshness Report",
          "",
          `App root: ${appRoot}`,
          "",
          "Feature Area           | Source Last Modified    | Docs Last Modified     | Status",
          "-----------------------|------------------------|------------------------|------------------",
        ];

        for (const entry of CONFIG.statusMap) {
          const srcPath = join(appRoot, ...entry.srcDir.split("/"));
          const docDir = join(docsRoot, entry.doc);
          const srcMtime = getLatestMtime(srcPath);

          // Find the first .md file in the doc folder (context-named, no longer README.md)
          let docMtime = 0;
          try {
            if (existsSync(docDir) && statSync(docDir).isDirectory()) {
              const mdFiles = readdirSync(docDir).filter(f => f.endsWith(".md"));
              if (mdFiles.length > 0) {
                docMtime = statSync(join(docDir, mdFiles[0])).mtimeMs;
              }
            }
          } catch {
            // Not accessible
          }

          let status;
          if (srcMtime === 0) {
            status = "no source";
          } else if (docMtime === 0) {
            status = "MISSING DOCS";
          } else if (srcMtime > docMtime) {
            status = "potentially-stale";
          } else {
            status = "up-to-date";
          }

          const label = entry.label.padEnd(23);
          const srcTs = formatTimestamp(srcMtime).padEnd(24);
          const docTs = formatTimestamp(docMtime).padEnd(24);
          lines.push(`${label}| ${srcTs}| ${docTs}| ${status}`);
        }

        return lines.join("\n");
      },
    },
    {
      name: "planning_status",
      description:
        "Audits the planning docs (implementation-plan.md and task-checklist.md) against the codebase. Reports last-synced date, stale sections, and items that may need updating based on file timestamps.",
      parameters: {
        type: "object",
        properties: {},
      },
      handler: async () => {
        const cwd = process.cwd();
        const appRoot = findAppRoot(cwd);

        if (!appRoot) {
          return `Could not locate ${CONFIG.appRoot} directory. Searched from: ${cwd}`;
        }

        const lines = [
          "# Planning Documentation Audit",
          "",
          `App root: ${appRoot}`,
          "",
        ];

        // Check each planning doc
        for (const docRelPath of CONFIG.planningDocs) {
          const docPath = join(appRoot, ...docRelPath.split("/"));
          const docName = docRelPath.split("/").pop();

          if (!existsSync(docPath)) {
            lines.push(`## ${docName} — ❌ MISSING`);
            lines.push(`Expected at: ${docPath}`);
            lines.push("");
            continue;
          }

          const docStat = statSync(docPath);
          const docMtime = docStat.mtimeMs;

          // Read the "Last synced" line
          let lastSynced = "unknown";
          try {
            const content = readFileSync(docPath, "utf-8");
            const match = content.match(/Last synced[^:]*:\s*(.+)/i);
            if (match) lastSynced = match[1].trim();
          } catch {
            // Ignore read errors
          }

          lines.push(`## ${docName}`);
          lines.push(`- Last modified: ${formatTimestamp(docMtime)}`);
          lines.push(`- Last synced (declared): ${lastSynced}`);
          lines.push("");
        }

        // Compare against key source directories
        lines.push("## Source Directory Status");
        lines.push("");
        lines.push(
          "Directory                   | Last Modified          | Review Recommended",
        );
        lines.push(
          "----------------------------|------------------------|-------------------",
        );

        // Get the oldest planning doc mtime as baseline
        let planningBaseline = Infinity;
        for (const docRelPath of CONFIG.planningDocs) {
          const docPath = join(appRoot, ...docRelPath.split("/"));
          if (existsSync(docPath)) {
            const mt = statSync(docPath).mtimeMs;
            if (mt < planningBaseline) planningBaseline = mt;
          }
        }
        if (planningBaseline === Infinity) planningBaseline = 0;

        for (const entry of CONFIG.planningCheckDirs) {
          const srcPath = join(appRoot, ...entry.dir.split("/"));
          const srcMtime = getLatestMtime(srcPath);
          const stale =
            srcMtime === 0
              ? "no source"
              : srcMtime > planningBaseline
                ? "⚠️ review recommended"
                : "✅ up-to-date";

          const label = entry.label.padEnd(28);
          const srcTs = formatTimestamp(srcMtime).padEnd(24);
          lines.push(`${label}| ${srcTs}| ${stale}`);
        }

        return lines.join("\n");
      },
    },
  ],
});
