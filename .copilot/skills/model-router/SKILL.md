---
name: model-router
description: "Intelligent LLM model selection — matches the right model to the right task based on capability scoring, cost constraints, and task requirements. Routes across 16+ models from Anthropic, OpenAI, and Google."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  version: "1.0.0"
  domain: workflow
  triggers: select model, choose LLM, model routing, which model, best model, model selection, route task, model recommendation
  role: expert
  scope: design
  platforms: copilot-cli, claude, gemini, codex
  output-format: recommendation
  related-skills: agent-orchestrator, prompt-engineer, memory-optimization
---

# Model Router

An intelligent model selection engine that matches the right LLM to the right task. Scores 16+ models across 10 capability dimensions, applies task-specific weight profiles, respects cost constraints, and logs decisions for progressive learning.

## When to Use This Skill

- Selecting which model to assign to a sub-agent in a multi-agent workflow
- Choosing a model override for a `task` tool call (`model` parameter)
- Evaluating cost vs quality trade-offs for a specific task type
- Planning fleet composition in agent-orchestrator workflows
- Reviewing whether current model assignments are optimal
- Adding a new model to the routing matrix after a provider release

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Model Strengths Matrix | `references/model-strengths-matrix.md` | Scoring models for a specific task |
| Routing Rules | `references/routing-rules.md` | Applying decision tree, weight profiles, or scoring formula |
| Task Classification | `references/task-classification.md` | Classifying an unfamiliar task into one of 12 categories |
| Learning Protocol | `references/learning-protocol.md` | Logging decisions, mining history, updating the matrix |

## Core Workflow

### Step 1 — Classify the Task

Determine what kind of work the model will perform.

1. **Identify the task** — What is the agent or operation trying to accomplish?
2. **Map to a category** — Assign one of the 12 task categories:
   `code-review` | `security-audit` | `test-generation` | `complex-implementation` | `refactoring` | `debugging` | `documentation` | `architecture-review` | `code-exploration` | `build-test-execution` | `planning-decomposition` | `prompt-engineering`
3. **Estimate complexity** — Assign a t-shirt size: S (simple lookup), M (focused analysis), L (multi-step reasoning), XL (architectural decision with many files).

> **If the task doesn't clearly map**, load `references/task-classification.md` for detailed category definitions and examples.

**✅ Checkpoint: Task has a category and complexity. If ambiguous, default to the category that demands higher reasoning.**

### Step 2 — Determine Constraints

Identify hard constraints that filter the candidate pool.

1. **Budget preference** — Is the user cost-sensitive?
   - `cost-optimized` → Eliminate Premium tier; prefer Fast/Cheap
   - `balanced` → All tiers eligible; cost is a scoring factor (default)
   - `quality-first` → Prefer Premium/Standard; cost deprioritized
2. **Parallelism** — Is this one of many agents in a fleet?
   - Parallel fleets prefer cheaper models (cost scales with agent count)
3. **Modification scope** — Does the agent write code or only read?
   - Write operations → Higher code generation score required
   - Read-only → Speed and cost efficiency matter more
4. **Domain sensitivity** — Is this fintech, security, or compliance-related?
   - Sensitive domains → Require top-tier reasoning depth (≥4)

**✅ Checkpoint: Budget mode set, parallelism noted, read/write classified, sensitivity assessed.**

### Step 3 — Score and Select

Match task requirements against model capabilities.

1. **Load the weight profile** for the task category (from Step 1).
   Each category emphasizes different capability dimensions.
2. **Score each eligible model** using:
   ```
   Score = Σ(dimension_weight × model_score) / cost_factor
   ```
   Where `cost_factor` adjusts by budget mode:
   - `cost-optimized`: cost_factor = output_price_per_M / 2
   - `balanced`: cost_factor = output_price_per_M / 10
   - `quality-first`: cost_factor = 1 (no cost penalty)
3. **Rank candidates** — Sort by score descending.
4. **Select** — Pick the top-ranked model. Note the runner-up as fallback.

> Load `references/model-strengths-matrix.md` for the full scoring matrix.
> Load `references/routing-rules.md` for detailed weight profiles and formula.

**✅ Checkpoint: Primary model selected with score. Fallback identified. Selection rationale is explainable.**

### Step 4 — Present Recommendation

Format the routing decision for consumption.

```
📊 Model Recommendation
Task: {task_description}
Category: {category} | Complexity: {S/M/L/XL}
Budget: {cost-optimized|balanced|quality-first}

✅ Recommended: {model_id} ({provider}, {tier})
   Score: {score} | Est. Cost: {cost_range}
   Strengths: {top 2-3 dimensions for this task}

🔄 Fallback: {fallback_model_id} ({tier})
   Score: {score} | Why fallback: {reason}
```

**✅ Checkpoint: Recommendation is clear, actionable, and includes fallback.**

### Step 5 — Log the Decision

Record the routing decision for progressive learning.

1. **Log via `document_insight`** (or `log_routing_decision` if extension available):
   - Category: `decision`
   - Tags: `['model-routing', '{task_category}', '{model_id}']`
   - Content: Task, category, model selected, score, constraints applied
2. **After task completion**, update the log with outcome (success/failure, turns needed).

> Load `references/learning-protocol.md` for the full 3-tier learning system.

**✅ Checkpoint: Decision logged. Outcome will be recorded post-execution.**

## Quick Reference — Common Routing Decisions

| Task | Budget | Recommended Tier | Reasoning |
|---|---|---|---|
| Code exploration / file lookup | Any | Fast/Cheap | Speed matters, reasoning depth doesn't |
| Build & test execution | Any | Fast/Cheap | Tool use + speed; minimal reasoning needed |
| Code review (PR) | Balanced | Standard | Good code understanding + reasonable cost |
| Security audit | Any | Standard–Premium | High reasoning depth non-negotiable |
| Complex implementation | Balanced | Standard | Code generation + reasoning balance |
| Architecture review | Quality | Premium | Maximum reasoning + context utilization |
| Documentation | Cost | Fast/Cheap–Standard | Instruction following > reasoning depth |
| Debugging | Balanced | Standard–Premium | Creative problem-solving + code understanding |

## Constraints

### MUST DO

- Always classify the task before selecting a model — never skip to selection
- Always identify a fallback model in case the primary is unavailable
- Score models using the dimension weights for the task category — not gut feeling
- Factor in cost when multiple models score similarly (within 10%)
- Log every routing decision for progressive learning
- Use `balanced` budget mode as the default when no preference is stated
- Consider parallelism — fleet of 5 agents should prefer cheaper models

### MUST NOT

- Do not hardcode model selections — always run the scoring algorithm
- Do not select Premium models for simple tasks (code-exploration, build-test-execution)
- Do not ignore domain sensitivity — fintech/security tasks need strong reasoning
- Do not skip the fallback — the primary model may be unavailable or rate-limited
- Do not modify the strengths matrix without empirical evidence (Tier 2/3 learning data)
- Do not use a single model for all tasks — that defeats the purpose of intelligent routing

## Integration Notes

### Copilot CLI
Use `suggest_model` extension tool for automated scoring, or read this skill manually.
Trigger with: `select model`, `which model`, `model routing`, `best model for`.

### Claude
Read this skill via `.claude/skills/model-router/SKILL.md` bridge. Follow Core Workflow steps 1-5.

### Gemini / Codex
Read this skill directly from `.github/skills/ai/model-router/SKILL.md`. Follow Core Workflow.

### Agent Orchestrator Integration
When planning a fleet (agent-orchestrator Step 2), run this skill's Steps 1-3 for each agent to select optimal models. The orchestrator's delegation plan should include the model assignment per agent.
