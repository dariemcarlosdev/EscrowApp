# Learning Protocol

> 3-tier progressive learning system for the Model Router.
> Uses existing NexSynapse infrastructure — no new tools or databases required.

## Overview

The Model Router improves over time through three learning tiers:

| Tier | Name | When | Data Source | Updates |
|---|---|---|---|---|
| 1 | Static Matrix | Day 1 | Published benchmarks, API docs | Manual, on new model release |
| 2 | Decision Logging | Every session | `document_insight` + MemPalace | Automatic, per routing decision |
| 3 | Session Mining | Periodic review | Session Store SQL queries | On-demand, retrospective |

Each tier feeds the next — logged decisions (Tier 2) provide data for mining (Tier 3), which refines the static matrix (Tier 1).

---

## Tier 1 — Static Strengths Matrix

### Purpose
Provide Day 1 value with curated model scores based on public data.

### Data Sources
- Provider documentation (Anthropic, OpenAI, Google)
- Published benchmark results (MMLU, HumanEval, SWE-Bench, etc.)
- API pricing pages
- Community experience reports

### Update Triggers
| Event | Action |
|---|---|
| New model released by a provider | Add a row to `model-strengths-matrix.md` |
| Major benchmark update published | Review and adjust affected scores |
| Tier 3 mining reveals systematic bias | Adjust scores with empirical evidence |
| Provider changes pricing | Update the Pricing Reference table |

### Update Process
1. Identify which model and dimensions are affected
2. Gather evidence (benchmark links, empirical data from Tier 3)
3. Adjust scores in `references/model-strengths-matrix.md`
4. Update the `Last updated` date
5. If the matrix is a protected file, run `nexsynapse_update_baselines`

---

## Tier 2 — Decision Logging

### Purpose
Build a corpus of routing decisions with outcomes for future analysis.

### How to Log (Any AI Model)

Use `document_insight` (available via NexSynapse extensions) or the equivalent:

```
Category: decision
Title: "Model routing: {task_category} → {model_id}"
Tags: ['model-routing', '{task_category}', '{model_id}', '{budget_mode}']
Content: |
  Task: {description}
  Category: {task_category} | Complexity: {S/M/L/XL}
  Budget: {budget_mode} | Parallel: {yes/no}
  Domain Sensitive: {yes/no}

  Selected: {model_id} (score: {score})
  Fallback: {fallback_model_id} (score: {score})
  Constraints Applied: {list of active constraints}

  Outcome: {pending → updated post-execution}
  Turns to complete: {N}
  Quality assessment: {excellent/good/adequate/poor}
```

### Outcome Recording

After the routed task completes, update the insight with:

| Field | Description |
|---|---|
| **Outcome** | success, partial, failure, or retry-needed |
| **Turns** | How many conversation turns the model needed |
| **Quality** | Subjective: excellent, good, adequate, poor |
| **Notes** | What worked well or poorly |

### Where Data Lives

| Storage | Path | Purpose |
|---|---|---|
| Insight Log | `NexSynapse/docs/insights/insight-log.md` | Persistent, searchable log |
| MemPalace | `wing_nexsynapse/room: agents` | Cross-session semantic recall |
| Session DB | `session.todos` table | Per-session tracking |

### Copilot CLI Extension Automation

If the `model-router` extension is available, use:
```
log_routing_decision(task, category, complexity, model, score, fallback, constraints)
```
This automatically writes to the insight log with consistent formatting.

---

## Tier 3 — Session Mining

### Purpose
Analyze cross-session model performance data to identify patterns, validate assumptions, and refine the matrix.

### SQL Queries for Session Store Analysis

#### 1. Model Usage Distribution

```sql
-- Which models are being used for which task types?
SELECT content, session_id, source_type
FROM search_index
WHERE search_index MATCH 'model-routing'
ORDER BY rank
LIMIT 50;
```

#### 2. Model Performance by Task Category

```sql
-- Find routing decisions with outcomes
SELECT content, session_id
FROM search_index
WHERE search_index MATCH 'model-routing AND (excellent OR good OR adequate OR poor)'
ORDER BY rank
LIMIT 30;
```

#### 3. Model Switches (Retry Patterns)

```sql
-- Find sessions where models were switched mid-task (indicates initial selection was wrong)
SELECT s.id, s.summary, t.user_message
FROM sessions s
JOIN turns t ON t.session_id = s.id
WHERE t.assistant_response LIKE '%model%override%'
   OR t.assistant_response LIKE '%switching to%'
   OR t.assistant_response LIKE '%retry with%'
ORDER BY s.created_at DESC
LIMIT 20;
```

#### 4. Cost Efficiency Analysis

```sql
-- Sessions with high turn counts (may indicate wrong model choice)
SELECT s.id, s.summary, COUNT(t.turn_index) as turns
FROM sessions s
JOIN turns t ON t.session_id = s.id
GROUP BY s.id, s.summary
HAVING turns > 15
ORDER BY turns DESC
LIMIT 20;
```

#### 5. Category-Specific Performance

```sql
-- How well do models perform on security audits specifically?
SELECT content, session_id
FROM search_index
WHERE search_index MATCH 'model-routing AND security-audit'
ORDER BY rank
LIMIT 20;
```

### Mining Workflow

1. **Run the queries** above against `session_store` database
2. **Aggregate results** — count outcomes per model per category
3. **Identify patterns:**
   - Models that consistently score "poor" for a category → lower dimension scores
   - Models that need retries → flag as less reliable for that task type
   - Models with excellent outcomes at low cost → promote in balanced mode
4. **Update Tier 1 matrix** with empirical evidence
5. **Log the analysis** as a `learning` insight for audit trail

### Copilot CLI Extension Automation

If the `model-router` extension is available, use:
```
mine_routing_history(category?, model?, days?)
```
Runs the analysis queries and returns a summary report.

---

## Learning Cycle Cadence

| Activity | Frequency | Effort |
|---|---|---|
| Tier 1: New model added | On provider release | 5 minutes — add one row |
| Tier 2: Decision logging | Every routing decision | Automatic — < 1 minute |
| Tier 3: Mining analysis | Weekly or biweekly | 10–15 minutes |
| Matrix score adjustment | When mining reveals patterns | 5 minutes per model |

---

## Anti-Rationalization Guard

To prevent confirmation bias in learning:

| Bias Risk | Mitigation |
|---|---|
| Overrating a model because you're familiar with it | Rely on dimension scores, not habit |
| Underrating a model because of one bad experience | Require 3+ data points before adjusting scores |
| Ignoring cost in quality-first mode | Always log cost alongside quality |
| Assuming newer = better | Score based on benchmarks, not version numbers |
| Not logging failures | Failures are the most valuable learning data — always log them |
