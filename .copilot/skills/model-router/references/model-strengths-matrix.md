# Model Strengths Matrix

> 16 models scored across 10 capability dimensions. Scores are 1–5 (1 = weak, 5 = best-in-class).
> Based on published benchmarks, API documentation, and empirical usage patterns.
> Last updated: 2025-07-13

## Scoring Legend

| Score | Meaning |
|---|---|
| 5 | Best-in-class — top tier for this dimension |
| 4 | Strong — reliable and effective |
| 3 | Adequate — gets the job done |
| 2 | Limited — noticeable gaps |
| 1 | Weak — avoid for this dimension |

## Capability Dimensions

| # | Dimension | Description |
|---|---|---|
| 1 | **Reasoning** | Complex multi-step logic, chain-of-thought, architectural analysis |
| 2 | **Code Gen** | Syntax accuracy, idiomatic patterns, working first-try code |
| 3 | **Code Read** | Reading/analyzing existing code, tracing flows, understanding intent |
| 4 | **Instruct** | Adhering to constraints, following complex instructions, format compliance |
| 5 | **Speed** | Response latency — higher = faster |
| 6 | **Cost** | Tokens-per-dollar value — higher = cheaper |
| 7 | **Context** | Effective use of large context windows (100K+) |
| 8 | **Tools** | Proficiency with function calling, MCP tools, structured output |
| 9 | **Creative** | Novel approaches, unconventional solutions, lateral thinking |
| 10 | **Consistent** | Reproducibility across similar tasks, deterministic output |

---

## Full Matrix

### Anthropic Models

| Model | Tier | Reasoning | Code Gen | Code Read | Instruct | Speed | Cost | Context | Tools | Creative | Consistent | Best For | Avoid For |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| claude-opus-4.6 | Premium | 5 | 5 | 5 | 5 | 2 | 1 | 5 | 5 | 5 | 5 | complex-impl, architecture-review, debugging | code-exploration, build-test-exec |
| claude-opus-4.5 | Premium | 5 | 5 | 5 | 5 | 2 | 1 | 5 | 5 | 5 | 5 | complex-impl, architecture-review, security-audit | code-exploration, build-test-exec |
| claude-sonnet-4.6 | Standard | 4 | 5 | 4 | 4 | 3 | 3 | 4 | 5 | 4 | 4 | code-review, refactoring, test-generation | — |
| claude-sonnet-4.5 | Standard | 4 | 4 | 4 | 4 | 3 | 3 | 4 | 4 | 4 | 4 | code-review, refactoring, planning | — |
| claude-sonnet-4 | Standard | 4 | 4 | 4 | 4 | 3 | 3 | 4 | 4 | 4 | 4 | code-review, documentation, refactoring | — |
| claude-haiku-4.5 | Fast | 3 | 3 | 3 | 3 | 5 | 4 | 3 | 3 | 2 | 3 | code-exploration, documentation, build-test-exec | complex-impl, architecture-review |

### OpenAI Models

| Model | Tier | Reasoning | Code Gen | Code Read | Instruct | Speed | Cost | Context | Tools | Creative | Consistent | Best For | Avoid For |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gpt-5.4 | Standard | 4 | 4 | 4 | 4 | 3 | 3 | 4 | 4 | 4 | 4 | planning, code-review, refactoring | — |
| gpt-5.3-codex | Standard | 4 | 5 | 4 | 4 | 3 | 3 | 4 | 4 | 3 | 4 | complex-impl, test-generation, refactoring | documentation |
| gpt-5.2-codex | Standard | 4 | 5 | 4 | 4 | 3 | 3 | 4 | 4 | 3 | 4 | complex-impl, test-generation, refactoring | documentation |
| gpt-5.2 | Standard | 4 | 4 | 4 | 4 | 3 | 3 | 4 | 4 | 4 | 4 | code-review, planning, debugging | — |
| gpt-5.1 | Standard | 4 | 4 | 4 | 4 | 3 | 3 | 4 | 4 | 3 | 4 | code-review, refactoring, documentation | — |
| gpt-5.4-mini | Fast | 3 | 3 | 3 | 3 | 5 | 5 | 3 | 3 | 2 | 3 | code-exploration, build-test-exec, documentation | complex-impl, security-audit |
| gpt-5-mini | Fast | 3 | 3 | 2 | 3 | 5 | 5 | 3 | 3 | 2 | 3 | code-exploration, build-test-exec | complex-impl, security-audit |
| gpt-4.1 | Fast | 3 | 3 | 3 | 3 | 4 | 4 | 3 | 3 | 3 | 3 | code-exploration, documentation, build-test-exec | complex-impl |

### Google Models

| Model | Tier | Reasoning | Code Gen | Code Read | Instruct | Speed | Cost | Context | Tools | Creative | Consistent | Best For | Avoid For |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini-2.5-pro | Standard | 4 | 4 | 4 | 4 | 3 | 4 | 5 | 4 | 4 | 3 | architecture-review, planning, code-review | — |
| gemini-2.5-flash | Fast | 3 | 3 | 3 | 3 | 5 | 5 | 4 | 3 | 2 | 3 | code-exploration, build-test-exec, documentation | complex-impl, security-audit |

---

## Pricing Reference

| Model | Tier | Input ($/M tokens) | Output ($/M tokens) | Notes |
|---|---|---|---|---|
| claude-opus-4.6 | Premium | $5.00 | $25.00 | Highest quality, highest cost |
| claude-opus-4.5 | Premium | $5.00 | $25.00 | Previous Opus generation |
| claude-sonnet-4.6 | Standard | $3.00 | $15.00 | Best quality/cost balance (Anthropic) |
| claude-sonnet-4.5 | Standard | $3.00 | $15.00 | Strong all-rounder |
| claude-sonnet-4 | Standard | $3.00 | $15.00 | Reliable, well-established |
| claude-haiku-4.5 | Fast | $1.00 | $5.00 | Budget Anthropic option |
| gpt-5.4 | Standard | $2.50 | $15.00 | Latest GPT |
| gpt-5.3-codex | Standard | — | — | Code-specialized, pricing varies |
| gpt-5.2-codex | Standard | — | — | Code-specialized, pricing varies |
| gpt-5.2 | Standard | $2.50 | $15.00 | Strong general-purpose |
| gpt-5.1 | Standard | $2.50 | $15.00 | Reliable GPT |
| gpt-5.4-mini | Fast | ~$0.25 | ~$2.00 | Ultra-cheap GPT |
| gpt-5-mini | Fast | ~$0.25 | ~$2.00 | Ultra-cheap GPT |
| gpt-4.1 | Fast | $2.00 | $8.00 | Older but capable |
| gemini-2.5-pro | Standard | $1.25 | $10.00 | Best cost/quality (Google) |
| gemini-2.5-flash | Fast | $0.30 | $2.50 | Ultra-cheap Google option |

---

## Tier Summary

| Tier | Models | Use For | Typical Cost |
|---|---|---|---|
| **Premium** | opus-4.6, opus-4.5 | Architecture review, complex debugging, security-critical analysis | $5/$25 per M |
| **Standard** | sonnet-4.x, gpt-5.x, codex, gemini-pro | Most development tasks — code review, implementation, testing | $1.25–$3/$10–$15 per M |
| **Fast/Cheap** | haiku-4.5, gpt-mini, gpt-4.1, gemini-flash | Exploration, build execution, documentation, parallel fleets | $0.25–$2/$2–$8 per M |

---

## How to Update This Matrix

1. **New model released** → Append a row to the appropriate provider table
2. **Benchmark data changes** → Adjust dimension scores with evidence (cite source)
3. **Empirical feedback** → Use Tier 2/3 learning data (see `learning-protocol.md`)
4. **Version this file** → Update the `Last updated` date at the top
5. **Refresh baselines** → If this file is baselined, run `nexsynapse_update_baselines`
