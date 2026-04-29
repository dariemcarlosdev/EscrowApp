# Anti-Rationalization Pattern — Skill Authoring Guide

> A template and guide for adding anti-rationalization tables to AI skills.
> Adopted from Addy Osmani's agent-skills, adapted for the NexTruzt.io 50-skill infrastructure.

## What Are Anti-Rationalization Tables?

AI agents (and human developers) systematically skip process steps by generating plausible-sounding excuses.
Anti-rationalization tables preemptively counter these excuses with factual rebuttals.

**Format:**

```markdown
## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "Excuse the agent uses to skip a step" | Factual counter-argument |
```

## Why This Pattern Works

1. **AI agents are trained on agreement** — they'll agree with any rationalization unless explicitly countered
2. **Named excuses are harder to use** — when the exact shortcut is described, the agent self-corrects
3. **Reduces "just this once" erosion** — process discipline degrades one exception at a time
4. **Observable in output** — reviewers can spot when an agent used a rationalization

## Template (Copy-Paste Starter)

```markdown
## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "This is simple, I don't need [skill step]" | Simple tasks benefit most from process — they're where shortcuts create debt |
| "I already know how to do this" | Knowing isn't doing. The skill ensures you don't skip verification. |
| "I'll do it properly next time" | There is no next time. This IS the time. |
| "The user didn't ask for this" | The user asked for quality. Process IS quality. |
| "This will slow me down" | Rework from skipping steps takes 10× longer than doing it right. |
```

## Domain-Specific Anti-Rationalizations (NexTruzt.io Fintech)

### Payment Operations
| Rationalization | Reality |
|---|---|
| "Idempotency keys aren't needed for this call" | Every payment mutation needs idempotency. Stripe retries happen. Double charges destroy trust. |
| "I'll add error handling later" | In fintech, unhandled errors mean money in limbo. Handle errors NOW. |
| "This amount calculation is straightforward" | Floating-point math and currency rounding have ruined businesses. Use decimal. Always. |

### Security
| Rationalization | Reality |
|---|---|
| "This endpoint doesn't need [Authorize]" | Default deny. Every endpoint. No exceptions. |
| "I'll move the secret to Key Vault later" | Secrets in source code get committed, pushed, and cached. Move it NOW. |
| "Input validation is overkill here" | Injection attacks target the endpoints you think are safe. Validate everything. |

### Testing
| Rationalization | Reality |
|---|---|
| "This is too trivial to test" | Trivial bugs in payment code cost real money. Test it. |
| "The integration test covers this" | Integration tests are slow and brittle. Unit tests catch regressions fast. |
| "I'll write tests after" | "After" never comes. Write the test first (RED), then implement (GREEN). |

### Architecture
| Rationalization | Reality |
|---|---|
| "I'll refactor to Clean Architecture later" | Dependency violations compound. A domain layer referencing EF Core spreads to every consumer. Fix now. |
| "One direct DbContext access won't hurt" | It sets a precedent. The next developer copies your shortcut. Use the repository. |
| "This doesn't need a MediatR handler" | Consistency matters more than convenience. Every business operation goes through MediatR. |

## Applying to Existing Skills

### Priority Skills to Update (5 most impactful)

1. **owasp-audit** — Security shortcuts are the most dangerous
2. **code-reviewer** — Review discipline erodes fastest
3. **tdd-coach** — Testing is the most commonly skipped process
4. **architecture-reviewer** — Architecture violations compound silently
5. **deployment-preflight** — Ship-it pressure creates the strongest rationalizations

### How to Add

1. Read the existing SKILL.md
2. Identify the 3-5 steps agents most commonly skip
3. Write a rationalization for each (the excuse an agent would use)
4. Write a reality rebuttal for each (factual, not preachy)
5. Add the `## Common Rationalizations` section after `## Core Workflow`
6. Keep to 5-8 rows — too many dilutes impact

### Quality Checklist

- [ ] Each rationalization sounds like something an AI would actually say
- [ ] Each reality is factual, not opinion ("costs 10× more" not "is bad practice")
- [ ] Domain-specific examples where relevant (Stripe, EF Core, Blazor)
- [ ] No more than 8 rationalizations per skill (signal-to-noise ratio)
- [ ] Table placed after Core Workflow, before Anti-Patterns
