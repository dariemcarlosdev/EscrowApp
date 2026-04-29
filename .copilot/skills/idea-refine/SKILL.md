---
name: idea-refine
description: "Refine vague ideas through structured divergent/convergent thinking before writing specs. Use when requirements are unclear or user describes solutions instead of problems."
---

# Idea Refine

> Transform vague ideas into concrete, testable requirements through systematic divergent/convergent analysis.

## When to Use

- User provides a solution ("Add a crypto wallet") without stating the problem
- Requirements are ambiguous or under-specified ("Make it better", "Add escrow features")
- Feature request conflicts with MVP scope or business model
- Stakeholders describe implementation details instead of desired outcomes
- Before writing a technical spec for a new feature

**When NOT to use:**
- Requirements are already well-defined with acceptance criteria
- Refining an existing spec (use spec-writer instead)
- Debugging or fixing existing functionality (use debugging-wizard)

## Core Workflow

### 1. Problem Framing (Divergent Start)
- ✅ **Checkpoint:** Extract the ACTUAL problem from the request
- Ask: "What problem are we solving? For whom? Why now?"
- Distinguish between user-stated solution and underlying need
- Identify stakeholders: who benefits, who pays, who uses?

**Questions to ask:**
- "What outcome does success look like for the user?"
- "What pain point does this solve?"
- "How do users currently solve this without our platform?"
- "What happens if we don't build this?"

```
Example:
User says: "Add Ethereum wallet support"
Real problem: "Consultants want faster payouts than Stripe's 2-7 day settlement"
```

### 2. Divergent Brainstorm (Generate Options)
- ✅ **Checkpoint:** 5+ potential solutions identified, no filtering yet
- Generate multiple approaches WITHOUT judging feasibility
- Think in terms of user value, not technical implementation
- Consider both high-tech and low-tech solutions
- Include "do nothing" or "manual process" as valid options

**Techniques:**
- "What if money wasn't a constraint?"
- "What would a 10x simpler version look like?"
- "How would [competitor/adjacent industry] solve this?"

```
Example Solutions for "Faster Payouts":
1. Add Ethereum on-chain escrow (Web3)
2. Negotiate express payout upgrade with Stripe ($2 fee for instant)
3. Offer manual wire transfer for consultants (24-hour SLA)
4. Partner with a crypto off-ramp service
5. Build instant payout on top of stablecoins
6. Accept that 2-7 days is industry standard, focus elsewhere
```

### 3. Convergent Filtering (Apply Gates)
- ✅ **Checkpoint:** Solutions scored against MVP, revenue, security, and feasibility gates
- Score each solution against:
  - **MVP Gate:** Does this generate revenue or validate core assumptions? (see mvp-gatekeeper)
  - **Revenue Gate:** Does this enable monetization or unlock a paying segment?
  - **Security Gate:** Does this introduce PCI-DSS, money transmitter, or compliance risk?
  - **Feasibility Gate:** Can we build this in one sprint with current team/tools?
  - **Risk Gate:** What's the blast radius if this fails?

| Solution | MVP | Revenue | Security | Feasibility | Score |
|---|---|---|---|---|---|
| Ethereum escrow | ❌ | ❌ | ⚠️ | ❌ | 1/5 |
| Stripe express payout | ✅ | ✅ | ✅ | ✅ | 5/5 |
| Manual wire transfer | ⚠️ | ⚠️ | ✅ | ✅ | 3/5 |

### 4. Constraint Mapping
- ✅ **Checkpoint:** Known blockers and dependencies documented
- List technical constraints (APIs, data model, existing architecture)
- List regulatory constraints (escrow licensing, money transmitter laws)
- List business constraints (budget, timeline, team capacity)
- List user constraints (onboarding friction, learning curve)

**Constraint Template:**
```
Technical: Stripe SDK supports express payouts via `instant_payout` flag
Regulatory: Instant payouts don't change escrow licensing status
Business: $2 fee per payout — must pass through to user or absorb
User: One-click opt-in, no additional KYC required
```

### 5. Requirements Extraction
- ✅ **Checkpoint:** Winning idea converted into testable acceptance criteria
- Select the highest-scoring solution
- Define success criteria (measurable outcomes)
- Draft user story in format: "As [role], I want [capability], so that [benefit]"
- List acceptance criteria as Given/When/Then scenarios
- Identify out-of-scope items explicitly

**Output Template:**
```markdown
## Feature: Instant Consultant Payouts

**User Story:**
As a consultant, I want to receive payment within 1 hour of project approval, 
so that I have faster access to earned funds.

**Acceptance Criteria:**
- [ ] Given a completed escrow transaction in "Released" status
- [ ] When the consultant opts into instant payout (pays $2 fee)
- [ ] Then funds arrive in their bank account within 60 minutes
- [ ] And the $2 fee is deducted from the payout amount
- [ ] And the consultant receives email confirmation with ETA

**Out of Scope for MVP:**
- Crypto/blockchain payouts
- Free instant payouts
- Payouts to debit cards
- Batch/scheduled instant payouts
```

### 6. Handoff to Spec-Writer
- ✅ **Checkpoint:** Clear requirements ready for spec-writer skill
- Pass refined requirements to spec-writer for detailed technical spec
- Include constraint notes and rejected alternatives (for context)
- Document any unresolved questions or assumptions

## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "This is obvious, I don't need to brainstorm" | Most "obvious" solutions miss edge cases or better alternatives. 5 minutes divergent thinking prevents days of rework. |
| "Let me just start coding" | Code without validated requirements is rework waiting to happen. You'll build the wrong thing fast. |
| "The user already knows what they want" | Users describe solutions, not problems. Find the problem first or you'll solve the wrong one. |
| "I'll refine as I go" | In-flight refinement creates scope creep and half-implemented features. Refine once, build once. |
| "This is blocking development" | Vague requirements block development MORE. 30 minutes here saves 8 hours debugging scope mismatch. |
| "MVP means we skip this step" | MVP means ruthless prioritization, not skipping thinking. Bad ideas ship faster without this step — they're still bad ideas. |

## Anti-Patterns

| Pattern | Problem | Fix |
|---|---|---|
| **Solution Fixation** | User says "add feature X" and you immediately spec feature X without questioning if it solves the real problem | Ask "what problem does X solve?" Extract the need, then brainstorm 3+ solutions |
| **First Idea Wins** | Pick the first plausible solution without generating alternatives | Force divergent phase — generate 5 ideas BEFORE filtering |
| **Analysis Paralysis** | Endless brainstorming without converging on a decision | Set a timebox: 15 minutes divergent, 15 minutes convergent. Ship something. |
| **Skipping Constraints** | Design a solution that violates regulatory, technical, or business constraints | Run constraint mapping BEFORE finalizing requirements |
| **Scope Creep During Refinement** | "While we're at it, let's also add…" | Document follow-on ideas separately. One feature at a time. |

## Red Flags

Abort and refine more if you observe:

- Requirements doc has no acceptance criteria (vague handwave)
- Spec includes phrases like "and other related features" or "etc."
- Multiple stakeholders describe the same feature differently
- Technical design started before problem validation
- Feature conflicts with existing business model or regulatory posture
- You can't explain the feature's ROI in one sentence
- The spec is longer than 2 pages but has no testable criteria

## Verification

Before handing off to spec-writer:

- [ ] Problem statement written (1-2 sentences max)
- [ ] 5+ solution approaches generated during divergent phase
- [ ] MVP gate applied — feature enables revenue or validates core hypothesis
- [ ] Security gate applied — no new compliance blockers introduced
- [ ] Feasibility confirmed — can ship in one sprint
- [ ] Constraints documented (technical, regulatory, business, user)
- [ ] User story written in "As/I want/So that" format
- [ ] 3+ acceptance criteria defined as testable scenarios
- [ ] Out-of-scope items explicitly listed
- [ ] Rejected alternatives documented with reasoning (prevents re-litigation)

## Integration Points

**Before this skill:**
- User provides vague feature request or "I want X" statement

**After this skill:**
- ✅ Pass refined requirements to `spec-writer` for detailed technical spec
- ✅ Pass feature to `mvp-gatekeeper` if prioritization is unclear
- ✅ Pass to `feature-forge` for task breakdown if requirements are large

**Chains well with:**
- `spec-writer` — converts refined requirements into detailed technical specs
- `mvp-gatekeeper` — validates that refined feature aligns with MVP scope
- `threat-modeler` — if security constraints were flagged during refinement
- `multi-agent-planner` — for complex features requiring cross-functional analysis
