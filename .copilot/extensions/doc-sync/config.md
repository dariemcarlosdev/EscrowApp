# Doc-Sync Extension — Configuration Guide

The doc-sync extension monitors file edits and reminds AI agents to keep documentation
in sync with source code changes. All project-specific mappings are defined in
`doc-sync.config.json` — no code changes needed to adopt this extension for a new project.

---

## How It Works

1. On startup, the extension loads `doc-sync.config.json` from the same directory as `extension.mjs`.
2. Config values are compiled into regex patterns and lookup tables used at runtime.
3. If the config file is missing or invalid, the extension falls back to built-in defaults and logs a warning.

---

## Config File Location

Place `doc-sync.config.json` in the same directory as `extension.mjs`:

```
.github/extensions/doc-sync/
├── extension.mjs           ← extension code (no project-specific data)
├── doc-sync.config.json    ← your project's mappings
└── config.md               ← this file
```

---

## Schema Reference

### `appRoot` (string, required)

The subdirectory name containing your application source and docs. The extension looks
for `cwd/{appRoot}` first, then falls back to `cwd` itself.

```json
"appRoot": "EscrowApp"
```

### `projectFile` (string, required)

A file that must exist at the app root to confirm it's the right directory (e.g., a
project file or `package.json`).

```json
"projectFile": "EscrowApp.csproj"
```

### `docsDir` (string, default: `"docs"`)

Relative path from `appRoot` to the documentation root directory.

```json
"docsDir": "docs"
```

### `planningDocs` (string[], default: `[]`)

Paths to planning/tracking documents relative to `appRoot`. The extension:
- Suppresses planning reminders when you edit these files directly.
- Reports their freshness in the `planning_status` tool.

```json
"planningDocs": [
  "docs/planning/implementation-plan.md",
  "docs/planning/task-checklist.md"
]
```

### `featureMap` (array of `{ pattern, doc }`, default: `[]`)

Maps source code path patterns to documentation folders. **Order matters** — more specific
patterns should come before general ones (e.g., `Services/Strategies` before `Services/`).

| Field | Type | Description |
|-------|------|-------------|
| `pattern` | string | Path fragment to match (forward slashes; auto-converted to cross-platform regex) |
| `doc` | string | Documentation folder relative to `docsDir` |

```json
"featureMap": [
  { "pattern": "Services/Strategies", "doc": "architecture/payment-strategies" },
  { "pattern": "Services/", "doc": "architecture/overview" }
]
```

### `watchedDirs` (string[], default: `[]`)

Top-level directory names that trigger feature doc reminders when files inside them are
edited. The extension checks if the file path contains `/{dirName}/` (case-insensitive).

```json
"watchedDirs": ["Features", "Services", "Models", "Components"]
```

### `planningAffectingDirs` (string[], default: `[]`)

Directory names whose edits trigger planning doc reminders. Typically a superset of
`watchedDirs` including data/migration directories.

```json
"planningAffectingDirs": ["Features", "Services", "Models", "Data", "Migrations"]
```

### `planningAffectingFiles` (string[], default: `[]`)

Specific filenames (matched at end of path) that trigger planning doc reminders.

```json
"planningAffectingFiles": ["Program.cs", "Startup.cs"]
```

### `planningAffectingPatterns` (string[], default: `[]`)

Freeform path fragments that trigger planning doc reminders. Slashes are converted to
cross-platform separators automatically.

```json
"planningAffectingPatterns": ["Tests/", ".csproj"]
```

### `statusMap` (array of `{ label, srcDir, doc }`, default: `[]`)

Defines the rows shown by the `docs_status` tool. Each entry compares a source directory's
last-modified time against its documentation README.

| Field | Type | Description |
|-------|------|-------------|
| `label` | string | Display name in the report |
| `srcDir` | string | Source directory relative to `appRoot` (supports `../` for sibling dirs) |
| `doc` | string | Documentation folder relative to `docsDir` |

```json
"statusMap": [
  { "label": "User Auth", "srcDir": "Features/Auth", "doc": "features/auth" },
  { "label": "Testing", "srcDir": "../MyApp.Tests", "doc": "cross-cutting/testing" }
]
```

### `planningCheckDirs` (array of `{ label, dir }`, default: `[]`)

Directories compared against planning docs in the `planning_status` tool.

| Field | Type | Description |
|-------|------|-------------|
| `label` | string | Display name in the report |
| `dir` | string | Directory relative to `appRoot` (supports `../`) |

```json
"planningCheckDirs": [
  { "label": "Features", "dir": "Features" },
  { "label": "Tests", "dir": "../MyApp.Tests" }
]
```

### `cooldowns` (object, optional)

Controls how often the extension reminds about documentation updates (in milliseconds).

| Field | Default | Description |
|-------|---------|-------------|
| `featureDocReminderMs` | `60000` (1 min) | Minimum interval between reminders for the same doc folder |
| `planningDocReminderMs` | `120000` (2 min) | Minimum interval between planning doc reminders |

```json
"cooldowns": {
  "featureDocReminderMs": 60000,
  "planningDocReminderMs": 120000
}
```

### `defaultDocFallback` (string, default: `"architecture/overview"`)

The doc folder used when a file path doesn't match any `featureMap` pattern.

```json
"defaultDocFallback": "architecture/overview"
```

---

## Adopting for a New Project

### Step 1: Copy the extension

Copy the entire `.github/extensions/doc-sync/` directory into your project.

### Step 2: Create your config

Create `doc-sync.config.json` with your project's values. Start with the minimal
example below and expand as needed.

### Step 3: Map your source to docs

For each source directory that has corresponding documentation:
1. Add an entry to `featureMap` (pattern → doc folder).
2. Add an entry to `statusMap` (for the `docs_status` report).
3. Add the top-level directory name to `watchedDirs`.

### Step 4: Configure planning docs (optional)

If you have planning/tracking documents:
1. Add their paths to `planningDocs`.
2. Add source directories to `planningAffectingDirs`.
3. Add entries to `planningCheckDirs`.

### Step 5: Verify

The extension validates on startup. Check the session log for any warnings. If the
config is missing or invalid, it falls back to built-in defaults (EscrowApp mappings).

---

## Example: Minimal Config (Simple Project)

```json
{
  "appRoot": ".",
  "projectFile": "package.json",
  "docsDir": "docs",
  "featureMap": [
    { "pattern": "src/auth", "doc": "features/auth" },
    { "pattern": "src/api", "doc": "features/api" }
  ],
  "watchedDirs": ["src"],
  "statusMap": [
    { "label": "Authentication", "srcDir": "src/auth", "doc": "features/auth" },
    { "label": "API Layer", "srcDir": "src/api", "doc": "features/api" }
  ],
  "defaultDocFallback": "overview"
}
```

## Example: Full Config (EscrowApp Reference)

See `doc-sync.config.json` in this directory for the complete EscrowApp configuration
with 16 feature mappings, 13 status entries, 8 planning check directories, and
full planning doc integration.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| "WARNING: Config file not found" in session log | `doc-sync.config.json` is missing | Create the file in the same directory as `extension.mjs` |
| "WARNING: Config missing required fields" | `appRoot` or `projectFile` not set | Add both required fields to the config |
| "WARNING: Failed to load doc-sync config" | Invalid JSON syntax | Validate the JSON (check for trailing commas, missing quotes) |
| No reminders triggered | `watchedDirs` doesn't include the edited directory | Add the directory name to `watchedDirs` |
| Wrong doc folder in reminders | `featureMap` order is wrong | Put more specific patterns before general ones |
| `docs_status` shows "no source" | `srcDir` path is incorrect | Verify the path relative to `appRoot` |
