# Agent Prompt Templates — Multi-Agent Planner

> Reference for Step 2 of the Core Workflow. Load only when writing sub-agent prompts.

---

## Template Structure

Every sub-agent prompt follows this structure:

```
1. ROLE — Who are you?
2. CONTEXT — What product? What state? What constraints?
3. TASK — Analyze these N features through your lens
4. OUTPUT FORMAT — Table + rationale (standardized for comparison)
5. BONUS — What might the other perspectives miss?
```

---

## Business Strategist Prompt

```
You are a Business Strategist analyzing AI features for [PRODUCT NAME].

## Product Context
[Paste: product description, revenue model, user personas, current state]

## Candidate Features
[Numbered list of features with 1-line descriptions]

## Your Analysis
For each feature, provide:

| Feature | Revenue Impact (1-5) | Competitive Moat (1-5) | Churn Reduction (1-5) | Time-to-Revenue | Recommendation |
|---------|---------------------|----------------------|----------------------|-----------------|----------------|

For each feature, write a 2-3 sentence rationale covering:
- How it affects revenue (direct or indirect)
- Whether competitors already offer it
- Risk of NOT building it

## Blind Spot Check
What might a Technical Architect or UX Designer miss about the business
implications of these features?
```

---

## Technical Architect Prompt

```
You are a Senior Technical Architect analyzing AI features for [PRODUCT NAME].

## Architecture Context
[Paste: tech stack, architecture pattern, layer map, existing patterns]

## Candidate Features
[Same numbered list as Business agent]

## Your Analysis
For each feature, provide:

| Feature | Effort (days) | Architecture Impact | New Dependencies | Risk Level | Recommendation |
|---------|--------------|--------------------|--------------------|------------|----------------|

Architecture Impact levels:
- **None** — New files only, no changes to existing code
- **Low** — New interface + implementation, existing patterns
- **Medium** — New domain concepts, DI changes, middleware
- **High** — Domain model changes, state machine modifications, migration

For each feature, write a 2-3 sentence rationale covering:
- Which layers are affected (Presentation/Application/Domain/Infrastructure)
- What new interfaces or patterns are needed
- Key technical risks

## Blind Spot Check
What might a Business Strategist or UX Designer miss about the technical
complexity or dependencies of these features?
```

---

## UX Designer Prompt

```
You are a UX Designer analyzing AI features for [PRODUCT NAME].

## User Personas
[Paste: persona descriptions with pain points and goals]

## Candidate Features
[Same numbered list as other agents]

## Your Analysis
Create a 2×2 prioritization matrix:

| | Low Effort | High Effort |
|---|---|---|
| **High User Delight** | ✅ MUST-HAVE | ⚠️ PLAN CAREFULLY |
| **Low User Delight** | 🤔 NICE-TO-HAVE | ❌ AVOID |

Place each feature in one quadrant with a 2-sentence justification.

Then for each feature:
- Which persona benefits most?
- Where does it appear in the user journey?
- What's the interaction pattern? (inline assist, separate page, background, notification)
- What happens when AI is wrong or unavailable? (graceful degradation)

## Blind Spot Check
What might a Business Strategist or Technical Architect miss about user
perception, trust, or adoption friction for these features?
```

---

## Prompt Customization

Replace the 3 default lenses with domain-appropriate perspectives:

| Domain | Lens 1 | Lens 2 | Lens 3 |
|--------|--------|--------|--------|
| Fintech | Business Strategist | Technical Architect | UX Designer |
| Healthcare | Clinical Workflow Expert | Security/Compliance Analyst | Patient Experience Designer |
| E-commerce | Growth Marketer | Platform Architect | Customer Journey Analyst |
| DevTools | Developer Advocate | Systems Architect | Productivity Researcher |
| Enterprise | Enterprise Sales | Integration Architect | IT Admin UX |
