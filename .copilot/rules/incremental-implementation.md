# Incremental Implementation Rules — Gemini Agent
# Source: .github/skills/code-quality/incremental-implementation/SKILL.md

## When Active
- Implementing any feature, fix, or refactor
- Building code from a plan or task list

## Core Loop (repeat for each task)
1. **Pick ONE task** — Smallest deliverable slice from the plan
2. **Load context** — Read relevant files, match existing patterns
3. **Write failing test** — RED phase (xUnit + FluentAssertions)
4. **Implement minimum code** — GREEN phase (pass the test, nothing more)
5. **Run full test suite** — `dotnet test` — no regressions
6. **Build** — `dotnet build` — zero warnings
7. **Commit** — Descriptive conventional commit message
8. **Mark complete** — Move to next task

## Hard Rules
- NEVER build more than one slice before testing
- A 1000-line uncommitted diff is a failure mode
- Match existing patterns in the codebase — don't invent new conventions
- If a test fails, fix it before moving on — never leave broken tests

## Anti-Rationalization
- "I'll test at the end" → Catching bugs at the end is the most expensive option
- "Too small to commit" → Small commits are GOOD — easy to review, revert, bisect
- "Build the whole feature first" → You're building risk, not value. Ship one slice.
- "Need to refactor first" → Implement first, refactor after. Working > perfect.

## Context: NexTruzt.io Patterns
- Vertical slices in Features/Escrow/ — one handler per folder
- Blazor code-behind: .razor + .razor.cs + .razor.css (always 3 files)
- MediatR commands/queries for all business operations
