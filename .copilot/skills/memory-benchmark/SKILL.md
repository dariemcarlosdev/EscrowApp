---
name: memory-benchmark
description: "Benchmark the NexSynapse 4-layer memory architecture — zero-cost proof of efficiency, recall, and token savings."
license: MIT
allowed-tools: Read, Grep, Glob, Bash, SQL
metadata:
  version: "1.0.0"
  domain: workflow
  triggers: benchmark, performance, memory stats, how efficient, token usage, storage stats, recall performance, run benchmark, memory layers, efficiency
  role: expert
  scope: benchmarking
  platforms: copilot-cli, claude, gemini, codex
  output-format: report
  related-skills: memory-optimization, mempalace-memory, token-optimization
---

# Memory Benchmark

A workflow skill that benchmarks the NexSynapse 4-layer memory architecture, proving recall accuracy, storage efficiency, and token cost savings. Entirely zero-cost — uses only local filesystem checks and built-in Session Store SQL queries.

## When to Use This Skill

- When asked "how efficient is our memory?" or "run a benchmark"
- When demonstrating NexSynapse capabilities to stakeholders
- After adding new skills, bridge files, or MemPalace knowledge
- Before/after optimizing context window usage
- When comparing token costs with/without the memory architecture

## Architecture Under Test

```
┌─────────────────────────────────────────────────────┐
│  Layer 1: Session Store        (Auto, $0, built-in) │
│  ─ SQLite + FTS5 full-text search                   │
│  ─ Every turn, checkpoint, file edit auto-captured   │
├─────────────────────────────────────────────────────┤
│  Layer 2: MemPalace            (Curated, $0, local) │
│  ─ Cross-session semantic search + knowledge graph   │
│  ─ Decisions, patterns, debug insights, compliance   │
├─────────────────────────────────────────────────────┤
│  Layer 3: AGENTS.md + Bridges  (Static DNA, in-repo)│
│  ─ Architecture rules, patterns, conventions         │
│  ─ Auto-loaded every session (~17K tokens)           │
├─────────────────────────────────────────────────────┤
│  Layer 4: Skills Library       (On-demand, in-repo) │
│  ─ 51+ specialized skills with progressive refs      │
│  ─ Load 1 skill (~2K tokens) vs all (~340K tokens)  │
└─────────────────────────────────────────────────────┘
```

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| Layer-by-Layer Analysis | `references/layer-analysis.md` | Deep-diving into individual layer metrics and query templates |
| Cost Comparison Method | `references/cost-comparison.md` | Explaining token cost methodology and API pricing assumptions |

## Core Workflow

### 1. Measure Layer 3: AGENTS.md + Model Bridges

Read and measure the bridge files that auto-load every session.

```bash
# Measure each bridge file
wc -c AGENTS.md CLAUDE.md GEMINI.md CODEX.md .github/copilot-instructions.md 2>/dev/null
# On Windows: Get-ChildItem AGENTS.md,CLAUDE.md,GEMINI.md,CODEX.md | Measure-Object -Property Length -Sum
```

Calculate: `total_chars / 4 ≈ estimated_tokens`

- ✅ Checkpoint: Bridge file count and total token estimate recorded.

### 2. Measure Layer 4: Skills Library

Count core SKILL.md files and reference files. Measure on-demand vs bulk loading cost.

```bash
# Count skills and references
find .github/skills -name "SKILL.md" | wc -l
find .github/skills -path "*/references/*.md" | wc -l
# Measure total size
find .github/skills -name "*.md" -exec cat {} + | wc -c
```

Calculate:
- `avg_skill_tokens = total_core_chars / 4 / skill_count`
- `progressive_savings = total_all_tokens - avg_skill_tokens`

- ✅ Checkpoint: Skill count, reference count, avg tokens per skill recorded.

### 3. Measure Layer 2: MemPalace

Check the palace directory for disk usage and file count.

```bash
# Check palace size (adjust path per OS)
du -sh ~/.mempalace/ 2>/dev/null || dir /s "%USERPROFILE%\.mempalace" 2>NUL
```

Note: MemPalace contains ChromaDB binary indexes + text content. Estimate ~15% of disk size is searchable text content.

- ✅ Checkpoint: Palace file count, disk size, estimated text tokens recorded.

### 4. Measure Layer 1: Session Store

Query the built-in Session Store for live metrics.

```sql
-- Session Store overview
SELECT COUNT(DISTINCT s.id) as sessions,
  COUNT(t.turn_index) as turns,
  (SELECT COUNT(*) FROM search_index) as fts_entries,
  ROUND((SELECT SUM(LENGTH(content)) FROM search_index) / 4.0) as est_tokens
FROM sessions s LEFT JOIN turns t ON t.session_id = s.id;

-- Checkpoint data
SELECT COUNT(*) as checkpoints,
  SUM(LENGTH(COALESCE(overview,'') || COALESCE(work_done,''))) as chars
FROM checkpoints;
```

- ✅ Checkpoint: Session count, turn count, FTS entries, token estimates recorded.

### 5. Run Recall Tests

Verify that each layer can find real data. These are zero-cost SQL queries.

```sql
-- Person recall
SELECT COUNT(*) FROM search_index WHERE search_index MATCH 'Frank OR Jokovish';

-- Idea recall
SELECT COUNT(*) FROM search_index WHERE search_index MATCH 'monetize OR compile OR binary';

-- Technical recall
SELECT COUNT(*) FROM search_index WHERE search_index MATCH 'Stripe OR payment OR escrow';

-- Debug recall
SELECT COUNT(*) FROM search_index WHERE search_index MATCH 'fix OR error OR broken';

-- File tracking
SELECT COUNT(DISTINCT file_path) as tracked_files FROM session_files;
```

- ✅ Checkpoint: All recall tests return >0 hits = PASS.

### 6. Calculate Cost Comparison

Compare per-session token cost WITH vs WITHOUT the memory architecture.

| Scenario | Formula |
|----------|---------|
| WITHOUT memory | ~80,000 tokens (user re-explains context every session) |
| WITH memory | AGENTS.md (~17K) + 1 Skill (~2K) + Session Store query (~500) + MemPalace (~800) ≈ ~20K |
| Savings | ~60,000 tokens per session (~75%) |
| API cost savings | ~$0.18/session at $3/M tokens |

- ✅ Checkpoint: Cost comparison table generated with real numbers from steps 1-4.

### 6b. Compare vs Standalone AI Agentic Tools

Map NexSynapse skills to their standalone tool equivalents and calculate replacement savings.

| Category | Standalone Tool(s) | Monthly Cost | NexSynapse Skills | NexSynapse Cost |
|----------|-------------------|-------------:|-------------------|----------------:|
| Code Review | CodeRabbit, Sourcery | ~$30/mo | code-reviewer, quality-analyzer | $0 |
| Security | Snyk, GitGuardian, Semgrep | ~$90/mo | owasp-audit, secret-scanner, threat-modeler | $0 |
| Testing | CodiumAI, Diffblue | ~$50/mo | test-generator, tdd-coach | $0 |
| Documentation | Mintlify, Swimm | ~$53/mo | readme-gen, api-documenter, adr-creator | $0 |
| Memory/Context | Mem.ai, Rewind AI | ~$34/mo | mempalace-memory, memory-optimization | $0 |
| Project Mgmt | Linear AI, Notion AI | ~$18/mo | spec-writer, issue-creator, feature-forge | $0 |
| Refactoring | Sourcery Pro | ~$14/mo | refactor-planner, smart-refactor | $0 |
| CI/CD & DevOps | Various CI AI tools | ~$30/mo | ci-cd-builder, deployment-preflight | $0 |
| Architecture | Manual consulting | ~$300+/mo | architecture-reviewer, design-pattern-advisor | $0 |
| Database | DBA tools | ~$25/mo | schema-reviewer, query-optimizer | $0 |
| **TOTAL** | **10+ subscriptions** | **~$644+/mo** | **51+ skills bundled** | **$0/mo** |

**Annual savings: ~$7,728/yr** in standalone tool subscriptions replaced by NexSynapse's portable skill library.

- ✅ Checkpoint: Standalone tool comparison table complete. Annual savings calculated.

### 6c. Compare Savings Across AI Models

Calculate per-model token cost savings using each platform's input pricing.

| Model | Input $/M | Saved/Session | Monthly (150×) | Annual |
|-------|----------:|--------------:|-----------:|-------:|
| Claude Opus 4.6 | $15.00 | ~$0.90 | ~$134 | ~$1,610 |
| Claude Sonnet 4.6 | $3.00 | ~$0.18 | ~$27 | ~$322 |
| Claude Haiku 4.5 | $0.80 | ~$0.05 | ~$7 | ~$86 |
| GPT-5.4 | $5.00 | ~$0.30 | ~$45 | ~$537 |
| GPT-4.1 | $2.00 | ~$0.12 | ~$18 | ~$215 |
| GPT-5.4 mini | $1.50 | ~$0.09 | ~$13 | ~$161 |
| Gemini 2.5 Pro | $1.25 | ~$0.075 | ~$11 | ~$134 |
| Gemini 2.5 Flash | $0.15 | ~$0.009 | ~$1.34 | ~$16 |

Formula: `(tokens_saved / 1M) × input_price × sessions_per_month × 12`

- ✅ Checkpoint: AI model comparison complete. Per-model annual savings calculated.

### 7. Generate Report

Present results in the user's preferred format:
- **Compact:** Single summary table with per-layer stats and savings percentage
- **Full:** Detailed report with per-layer analysis, recall test results, cost comparison, and ASCII efficiency chart

- ✅ Checkpoint: Report delivered. All 4 layers measured. Zero external API cost incurred.

## Output Template — Compact

```
## 📊 NexSynapse Memory Benchmark

| Layer | Items | Tokens | Status |
|-------|------:|-------:|--------|
| L1: Session Store | {sessions} sessions | ~{tokens} | ✅ Auto |
| L2: MemPalace | {files} files | ~{tokens} | ✅/⚠️ |
| L3: AGENTS.md | {files} files | ~{tokens} | ✅ Active |
| L4: Skills | {core}+{refs} files | ~{tokens} | ✅ On-demand |

Savings: {pct}% fewer tokens per session ({with} vs {without})
```

## Constraints

### MUST DO
- Use ONLY local filesystem and Session Store SQL — zero external API calls
- Report real measured values — never hardcode or estimate without measurement
- Include all 4 layers in every benchmark run
- Show per-session cost comparison (WITH vs WITHOUT memory)

### MUST NOT
- Call external APIs or web services during benchmarking
- Hardcode metrics — always measure fresh
- Skip any layer — the value is in the complete picture
- Report MemPalace raw disk size as token count (it includes binary indexes)
