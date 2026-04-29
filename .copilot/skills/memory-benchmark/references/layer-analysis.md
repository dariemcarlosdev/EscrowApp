# Layer-by-Layer Analysis — Memory Benchmark Reference

Deep-dive analysis templates for each of the 4 memory layers.

## Layer 1: Session Store — Detailed Analysis

The Session Store is a built-in SQLite database with FTS5 full-text search. It auto-captures every conversation turn, checkpoint, and file edit.

### Key Tables

| Table | Contents | Query Pattern |
|-------|----------|---------------|
| `sessions` | Session metadata (id, cwd, branch, summary) | `SELECT * FROM sessions ORDER BY updated_at DESC` |
| `turns` | Full conversation history (user + assistant) | `JOIN turns t ON t.session_id = s.id` |
| `checkpoints` | Compressed session milestones | `SELECT overview, work_done FROM checkpoints` |
| `session_files` | Files created/edited per session | `SELECT file_path, tool_name FROM session_files` |
| `session_refs` | Commits, PRs, issues linked to sessions | `SELECT ref_type, ref_value FROM session_refs` |
| `search_index` | FTS5 virtual table for full-text search | `WHERE search_index MATCH 'keyword'` |

### Benchmark Queries

```sql
-- Size and coverage
SELECT 
  COUNT(DISTINCT s.id) as total_sessions,
  COUNT(t.turn_index) as total_turns,
  (SELECT COUNT(*) FROM search_index) as fts_entries,
  (SELECT SUM(LENGTH(content)) FROM search_index) as total_chars,
  ROUND((SELECT SUM(LENGTH(content)) FROM search_index) / 4.0) as est_tokens
FROM sessions s LEFT JOIN turns t ON t.session_id = s.id;

-- Recall accuracy test suite
SELECT 'Person' as test, COUNT(*) as hits FROM search_index WHERE search_index MATCH 'Frank OR Jokovish'
UNION ALL SELECT 'Ideas', COUNT(*) FROM search_index WHERE search_index MATCH 'monetize OR compile OR binary'
UNION ALL SELECT 'Technical', COUNT(*) FROM search_index WHERE search_index MATCH 'Stripe OR payment OR escrow'
UNION ALL SELECT 'Debug', COUNT(*) FROM search_index WHERE search_index MATCH 'fix OR error OR broken'
UNION ALL SELECT 'Files', COUNT(DISTINCT file_path) FROM session_files;

-- Per-query token cost (how expensive is recall?)
SELECT 'Avg turn size' as metric,
  ROUND(AVG(LENGTH(user_message))) as avg_user_chars,
  ROUND(AVG(LENGTH(COALESCE(assistant_response,'')))) as avg_ai_chars
FROM turns;

-- Timeline reconstruction
SELECT DATE(s.created_at) as day, COUNT(*) as sessions
FROM sessions s GROUP BY day ORDER BY day DESC LIMIT 14;
```

### What Makes It Special

- **Zero-cost capture:** Everything is recorded automatically — no user effort
- **FTS5 keyword search:** Fast pattern matching across all indexed content
- **Query expansion required:** Not semantic search — expand queries to synonyms
- **Metadata is cheap:** SQL queries return structured data (~50-500 tokens) vs raw content (~5K+ tokens)

## Layer 2: MemPalace — Detailed Analysis

MemPalace provides curated, semantic cross-session memory via an MCP server.

### Palace Structure

Organized as Wings → Rooms → Drawers:
- **Wings** = major knowledge domains (escrowapp, nexsynapse, dotnet)
- **Rooms** = topic areas (architecture, payments, debugging, security)
- **Drawers** = individual knowledge items (decisions, patterns, fixes)

### Storage Composition

| Component | Disk % | Contains |
|-----------|-------:|----------|
| ChromaDB index | ~60% | Vector embeddings for semantic search |
| ChromaDB metadata | ~25% | Drawer metadata, timestamps, tags |
| Palace text content | ~15% | Actual knowledge items (readable text) |

### Benchmark Metrics

- **File count:** Number of files in `~/.mempalace/`
- **Disk size:** Total bytes (includes binary indexes)
- **Text tokens:** ~15% of disk size / 4 (conservative estimate)
- **Drawer count:** Via `mempalace_browse_palace` if MCP connected

## Layer 3: AGENTS.md + Model Bridges — Detailed Analysis

Static project DNA files that auto-load every session.

### Files Measured

| File | Purpose | Loaded By |
|------|---------|-----------|
| `AGENTS.md` | Universal project instructions | All models |
| `CLAUDE.md` | Claude-specific reasoning guidance | Claude |
| `GEMINI.md` | Gemini-specific exploration patterns | Gemini |
| `CODEX.md` | Codex-specific instructions | Codex |
| `.github/copilot-instructions.md` | Copilot CLI instructions | Copilot |

### Cost Profile

- **Fixed cost per session:** Always loaded, always consumed
- **Value proposition:** Eliminates need to re-explain architecture, patterns, conventions
- **Token investment:** ~17K tokens that save ~30K+ tokens of manual re-explanation
- **ROI:** Positive from session 1 — pays for itself every session

## Layer 4: Skills Library — Detailed Analysis

51+ specialized skills with progressive reference loading.

### Progressive Disclosure Architecture

```
Level 0: CATALOG.md              ~500 tokens  (skill discovery)
Level 1: SKILL.md                ~2K tokens   (core methodology)
Level 2: references/topic.md     ~1.4K tokens (deep-dive on specific sub-task)
```

### Key Metrics

| Metric | How to Measure |
|--------|----------------|
| Core skills | `find .github/skills -name "SKILL.md" \| wc -l` |
| References | `find .github/skills -path "*/references/*.md" \| wc -l` |
| Total tokens (all) | Sum all .md files in skills directory |
| Avg per skill | Total core tokens / skill count |
| Avg per reference | Total ref tokens / reference count |

### Savings Calculation

```
WITHOUT progressive disclosure:
  Load all 51 skills + 169 references = ~340K tokens

WITH progressive disclosure:
  Load 1 skill = ~2K tokens
  + 1 reference if needed = ~1.4K tokens
  Total = ~3.4K tokens

SAVINGS = 340K - 3.4K = ~336.6K tokens (99%)
```
