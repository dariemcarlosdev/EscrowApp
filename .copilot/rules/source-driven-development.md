# Source-Driven Development Rules — Gemini Agent
# Source: .github/skills/research/source-driven-development/SKILL.md

## When Active
- Using any external API, library, or framework
- Before implementing with a package you haven't verified

## Trust Hierarchy (most trusted → least)
| Source | Trust | Action |
|---|---|---|
| Official docs (Microsoft Learn, Stripe API) | High | Use directly |
| Project source code (existing usage) | High | Follow patterns |
| API reference / type definitions | High | Verify signatures |
| Blog posts, tutorials | Medium | Cross-ref with official |
| Stack Overflow | Low | Verify against docs |
| AI training data / memory | Lowest | ALWAYS verify |

## Workflow
1. **Identify** the API/library/framework being used
2. **Find official docs** — not blog posts, not tutorials
3. **Read the relevant section** — verify the API exists and matches your version
4. **Cross-reference** with existing codebase usage (grep for examples)
5. **Implement** using verified APIs only
6. **Test** — confirm behavior matches documentation

## Anti-Rationalization
- "I know this API" → APIs change between versions. .NET 10 ≠ .NET 8. Check.
- "I'll look it up if it fails" → 2 min reading docs saves 20 min debugging.
- "The AI knows the API" → Training cutoff dates exist. Stripe SDK v47 may differ.

## Critical for NexTruzt.io
- Stripe SDK methods — verify against https://stripe.com/docs/api
- EF Core APIs — verify against current .NET 10 docs
- Blazor lifecycle — OnInitializedAsync vs OnParametersSetAsync behavior
