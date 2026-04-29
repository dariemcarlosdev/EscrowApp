# Routing Rules

> Decision tree, task-to-dimension weight profiles, and scoring formula for intelligent model selection.
> Used by the Model Router skill's Step 2 and Step 3.

## Decision Tree

```
Task arrives
    │
    ├─ 1. CLASSIFY → Assign one of 12 task categories
    │       (see task-classification.md for definitions)
    │
    ├─ 2. COMPLEXITY → Estimate S / M / L / XL
    │       S = simple lookup, single file
    │       M = focused analysis, few files
    │       L = multi-step reasoning, many files
    │       XL = architectural decision, cross-cutting
    │
    ├─ 3. CONSTRAINTS → Apply hard filters
    │   │
    │   ├─ Budget mode?
    │   │   ├─ cost-optimized  → Exclude Premium tier
    │   │   ├─ balanced        → All tiers eligible (default)
    │   │   └─ quality-first   → Prefer Premium/Standard
    │   │
    │   ├─ Parallel fleet?
    │   │   ├─ Yes (3+ agents) → Prefer Fast/Cheap tier
    │   │   └─ No (solo agent) → All tiers eligible
    │   │
    │   ├─ Code modification?
    │   │   ├─ Writes code     → Require Code Gen ≥ 4
    │   │   └─ Read-only       → No minimum Code Gen
    │   │
    │   └─ Domain sensitive? (fintech, security, compliance)
    │       ├─ Yes → Require Reasoning ≥ 4
    │       └─ No  → No minimum Reasoning
    │
    ├─ 4. SCORE → Calculate weighted score per eligible model
    │       Score = Σ(weight_i × score_i) / cost_factor
    │
    ├─ 5. RANK → Sort by score descending
    │       If top-2 within 10% → pick the cheaper one
    │
    └─ 6. SELECT → Primary + Fallback
            Primary = rank #1
            Fallback = rank #2 (or next cheaper tier)
```

---

## Task-to-Dimension Weight Profiles

Each task category emphasizes different capability dimensions. Weights are 0–5 (0 = irrelevant, 5 = critical).

| Category | Reasoning | Code Gen | Code Read | Instruct | Speed | Cost | Context | Tools | Creative | Consistent |
|---|---|---|---|---|---|---|---|---|---|---|
| `code-review` | 4 | 2 | 5 | 3 | 3 | 3 | 3 | 2 | 2 | 4 |
| `security-audit` | 5 | 2 | 5 | 4 | 2 | 2 | 4 | 3 | 3 | 4 |
| `test-generation` | 3 | 5 | 3 | 4 | 3 | 3 | 3 | 2 | 2 | 4 |
| `complex-implementation` | 5 | 5 | 4 | 4 | 2 | 2 | 4 | 4 | 4 | 4 |
| `refactoring` | 4 | 4 | 5 | 3 | 2 | 3 | 3 | 3 | 3 | 4 |
| `debugging` | 5 | 3 | 5 | 3 | 2 | 2 | 4 | 4 | 4 | 3 |
| `documentation` | 2 | 2 | 3 | 4 | 4 | 4 | 2 | 1 | 2 | 3 |
| `architecture-review` | 5 | 2 | 4 | 3 | 2 | 2 | 5 | 3 | 4 | 4 |
| `code-exploration` | 2 | 1 | 3 | 2 | 5 | 5 | 2 | 4 | 1 | 3 |
| `build-test-execution` | 1 | 1 | 1 | 2 | 5 | 5 | 1 | 4 | 1 | 3 |
| `planning-decomposition` | 5 | 2 | 3 | 4 | 2 | 3 | 4 | 2 | 4 | 3 |
| `prompt-engineering` | 4 | 2 | 3 | 5 | 2 | 3 | 3 | 2 | 5 | 3 |

---

## Scoring Formula

### Basic Formula

```
Score(model, task) = Σ(weight[dim] × model_score[dim]) / cost_factor(model)
```

Where:
- `weight[dim]` = the weight for each dimension from the task's weight profile
- `model_score[dim]` = the model's score for that dimension (from strengths matrix)
- `cost_factor(model)` = adjusts for cost based on budget mode

### Cost Factor by Budget Mode

| Budget Mode | Cost Factor Formula | Effect |
|---|---|---|
| `cost-optimized` | `output_price_per_M_tokens / 2` | Heavy cost penalty — cheaper models win |
| `balanced` | `output_price_per_M_tokens / 10` | Moderate cost consideration |
| `quality-first` | `1` | No cost penalty — pure quality ranking |

### Worked Example

**Task:** Security audit of a payment handler
**Category:** `security-audit`
**Budget:** `balanced`

**Scoring claude-sonnet-4.6 ($15/M output):**

```
Weight profile: Reasoning=5, CodeGen=2, CodeRead=5, Instruct=4, Speed=2, Cost=2, Context=4, Tools=3, Creative=3, Consistent=4

Model scores:  Reasoning=4, CodeGen=5, CodeRead=4, Instruct=4, Speed=3, Cost=3, Context=4, Tools=5, Creative=4, Consistent=4

Score = (5×4) + (2×5) + (5×4) + (4×4) + (2×3) + (2×3) + (4×4) + (3×5) + (3×4) + (4×4)
      = 20 + 10 + 20 + 16 + 6 + 6 + 16 + 15 + 12 + 16
      = 137

Cost factor = 15 / 10 = 1.5

Final Score = 137 / 1.5 = 91.3
```

**Scoring claude-opus-4.6 ($25/M output):**

```
Model scores:  Reasoning=5, CodeGen=5, CodeRead=5, Instruct=5, Speed=2, Cost=1, Context=5, Tools=5, Creative=5, Consistent=5

Score = (5×5) + (2×5) + (5×5) + (4×5) + (2×2) + (2×1) + (4×5) + (3×5) + (3×5) + (4×5)
      = 25 + 10 + 25 + 20 + 4 + 2 + 20 + 15 + 15 + 20
      = 156

Cost factor = 25 / 10 = 2.5

Final Score = 156 / 2.5 = 62.4
```

**Result:** Sonnet 4.6 (91.3) beats Opus 4.6 (62.4) in balanced mode because the cost penalty outweighs the quality difference. In `quality-first` mode, Opus would win (156 vs 137).

---

## Complexity Adjustments

Complexity shifts the weight profile to emphasize different dimensions:

| Complexity | Adjustment |
|---|---|
| **S** (Simple) | Speed +2, Cost +2, Reasoning −2 |
| **M** (Medium) | No adjustment (use base profile) |
| **L** (Large) | Reasoning +1, Context +1, Speed −1 |
| **XL** (Extra Large) | Reasoning +2, Context +2, Creative +1, Speed −2, Cost −2 |

Apply adjustments to the base weight profile before scoring. Clamp values to 0–5.

---

## Tie-Breaking Rules

When two models score within 10% of each other:

1. **Prefer the cheaper model** — cost savings compound across sessions
2. **If same tier, prefer the newer model** — likely better benchmark performance
3. **If same provider, prefer the model with better tool use** — tool calling quality varies
4. **If still tied, prefer the model you have empirical data on** — known > unknown

---

## Override Rules

These rules take precedence over scoring in specific situations:

| Condition | Override |
|---|---|
| Task is `build-test-execution` | Always use Fast/Cheap tier (Haiku, mini, flash) |
| Task is `code-exploration` with no writes | Always use Fast/Cheap tier |
| Task is `security-audit` on payment code | Minimum Standard tier (Reasoning ≥ 4) |
| Task is `complex-implementation` on fintech domain | Minimum Standard tier |
| Fleet of 5+ parallel agents | Cap at Standard tier for any individual agent |
| User explicitly specified a model | Use user's choice — override is king |

---

## Budget Mode Selection Guide

| Signal | Recommended Mode |
|---|---|
| User says "quick look" / "just check" / "explore" | `cost-optimized` |
| User says nothing about cost | `balanced` (default) |
| User says "thorough" / "deep analysis" / "make sure" | `quality-first` |
| Parallel fleet (3+ agents) | `cost-optimized` per agent |
| Security or compliance task | `quality-first` regardless of user preference |
| Production code modification | `balanced` or `quality-first` |
