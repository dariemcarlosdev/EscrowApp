# Token-Optimized Authoring — Rules

> Full workflow: `.github/skills/workflow/token-optimization/SKILL.md`
> Companion skill: `memory-optimization` (session-level optimization)

## When to Apply

Apply these rules when creating or modifying any documentation, skill, or instruction file.

## Authoring Rules

1. **Self-containment:** Every file must be independently loadable — reader needs no other file to understand it
2. **Cross-reference, don't duplicate:** Use links to other docs — never copy their content
3. **Progressive disclosure:** Most important information in the first 20 lines
4. **Tables over prose:** Use tables for structured data (40–60% fewer tokens than equivalent paragraphs)
5. **Staleness detection:** Include `Last synced` date, `Status`, or `version` on every artifact
6. **Size budgets:** Feature docs ≤ 8 KB, ADRs ≤ 6 KB, Skill cores ≤ 10 KB, References ≤ 5 KB

## Skill Structure

```
.github/skills/{category}/{skill-name}/
├── SKILL.md              ← Core workflow (6–10 KB). Agent reads ONLY this first.
└── references/           ← Deep-dives (3–5 KB each). Loaded ONLY when needed.
```

## Anti-Patterns

| Anti-Pattern | Fix |
|---|---|
| Monolith doc > 15 KB | Split into main + references |
| Copy-paste across docs | Link to single source |
| Prose where table fits | Convert to table |
| Buried decisions | Move decision table to top |
| Missing status header | Add `Last synced` date |
| Load all references upfront | Lazy-load by workflow step |

## Cross-Reference Format

```markdown
> Cross-references: [Related Doc](../relative/path.md) · [Another](../other/path.md)
```
