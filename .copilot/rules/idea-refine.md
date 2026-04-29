# Idea Refinement Rules — Gemini Agent
# Source: .github/skills/project-management/idea-refine/SKILL.md

## When Active
- User presents a vague idea, feature request, or "what if we..."
- Before spec-writer — ideas must be refined first

## Workflow (5 Steps)
1. **Frame the Problem** — "What problem? For whom? What's the cost of not solving it?"
2. **Diverge** — Generate 5+ approaches without judging. Quantity over quality.
3. **Converge** — Filter through MVP gates: Does it generate revenue? Is it a security need? Can we launch without it?
4. **Extract Requirements** — Convert winning idea into acceptance criteria with measurable outcomes
5. **Hand Off** — Output ready for spec-writer skill

## Anti-Rationalization
- "This is obvious" → Most "obvious" solutions miss edge cases. Brainstorm anyway.
- "Let me just code it" → Code without requirements = rework. Frame the problem first.
- "The user knows what they want" → Users describe solutions, not problems. Find the problem.

## MVP Gate (from mvp-gatekeeper)
- Does user see/interact with this? → Build it
- Does the app crash without it? → Build it
- Is this nice-to-have for v1? → Defer it
