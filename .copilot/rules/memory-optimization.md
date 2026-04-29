# Memory & Context Optimization Rules

## Load Only What You Need

- **Never bulk-read directories** — use search to find files first, then read only relevant ones
- **Use line ranges** to read specific sections instead of full files
- **Progressive disclosure:** find files → count matches → read specific matches → deep dive
- **Batch parallel reads** — when you need multiple files, read them all in one turn

## Avoid Context Pollution

- Suppress verbose output (`--quiet`, `--no-pager`, pipe to `head`)
- Don't re-read files already seen in this session unless modified
- Don't echo file contents back unless asked
- On build/test success: report summary only, not full logs

## Search Efficiency

```
✅ Find files first, then read:    grep --files-with-matches → view specific file
❌ Read everything then search:     view entire directory tree
```

## Read Order Priority (most context-efficient first)

1. `docs/{feature}/` — high-level understanding, cheapest
2. Interface/contract files — understand API surface
3. MediatR command/handler — understand business flow
4. Implementation — only if you need internals
5. Tests — only if verifying behavior

## Anti-Patterns

- ❌ Reading entire files just to search them — use grep directly
- ❌ Exploratory full reads without a specific question
- ❌ Re-reading files you just edited — you know what's in them
- ❌ Sequential single-file reads — batch parallel reads
- ❌ Ignoring `docs/` when feature documentation exists
- ❌ Global unrestricted grep — always scope to relevant directories

## Scoped Search Directories

| Change Type | Search In |
|------------|-----------|
| UI changes | `Components/` |
| Business logic | `Features/` |
| Data access | `Data/` |
| Payment flow | `Services/Strategies/` |
| Domain model | `Models/`, `Events/` |
