# Knowledge-First Session Methodology

> **Purpose:** Enforce knowledge-aware workflows to prevent systematic gaps in institutional memory usage.

## Problem Statement

**Issue Observed:** AI agents default to immediate problem-solving without checking accumulated institutional knowledge, leading to:
- ❌ Duplicated debugging efforts
- ❌ Missed architectural context
- ❌ Inconsistent with prior decisions
- ❌ Loss of cross-session learnings

## Core Workflow

### Phase 1: Knowledge Audit (Pre-Execution)
```bash
# Step 1: Check available knowledge repositories
palace_status

# Step 2: Domain-specific knowledge search
mempalace_search "{primary_domain} {task_keywords}"
# Examples:
# - mempalace_search "authentication Blazor ASP.NET Identity"
# - mempalace_search "Stripe payment hold release strategy"
# - mempalace_search "Clean Architecture CQRS MediatR"

# Step 3: Cross-session task continuity
session_tracker_status  # or SQL: SELECT * FROM todos WHERE status = 'pending'
```

### Phase 2: Context Integration
- ✅ **Review found knowledge** before making architectural decisions
- ✅ **Identify knowledge gaps** — what's missing that we should research?
- ✅ **Align with prior patterns** — maintain consistency with established approaches

### Phase 3: Informed Execution
- 🎯 **Proceed with full context** — leverage institutional memory
- 📝 **Document deviations** — if departing from prior patterns, explain why
- 🔍 **Reference prior decisions** — link current work to established ADRs/insights

### Phase 4: Knowledge Capture (Post-Execution)
```bash
# Step 1: Capture key insights for institutional memory
save_insight 
# - type: decision (ADR/trade-offs)
# - type: pattern (reusable code patterns) 
# - type: debug (root cause/fixes)
# - type: security (OWASP findings)

# Step 2: Update cross-session state
# SQL session tracker OR session_tracker_update
```

## Integration Points

### With Existing Skills
| Skill | Knowledge-First Enhancement |
|-------|---------------------------|
| `debugging-wizard` | Search `room_debugging` before investigating |
| `architecture-reviewer` | Search `room_decisions` + `room_architecture` first |
| `code-reviewer` | Search `room_patterns` + `room_security` for standards |
| `owasp-audit` | Search `room_security` for prior vulnerability patterns |

### With Session Types
| Session Type | Auto-Search Domains |
|--------------|-------------------|
| **Validation/Audit** | Prior decisions, known issues, debugging patterns |
| **Implementation** | Architecture patterns, established approaches |
| **Debug/Fix** | Previous fixes, root causes, similar issues |
| **Planning** | ADRs, trade-offs, architectural decisions |

## Enforcement Rules

### 🚫 Blocked Actions Without Knowledge Check
- Creating new implementations without pattern search
- Debugging without checking prior similar issues
- Architecture decisions without ADR review  
- Documentation changes without context validation

### ✅ Required Evidence
- **"Based on MemPalace search for '{domain}', found..."**
- **"No prior patterns found for X, establishing new approach..."**
- **"Consistent with prior decision in room_decisions: '{insight}'..."**

## Success Metrics

- ✅ **Zero missed institutional knowledge** — relevant insights applied
- ✅ **Consistent patterns** — new work aligns with established approaches  
- ✅ **Knowledge accumulation** — insights captured for future sessions
- ✅ **Cross-session continuity** — seamless handoffs between sessions

## Reference

**Created:** Response to systematic knowledge gap observed in EscrowApp Track B session (2026-04-16)
**Integration:** Mandatory for all NexTruzt.io EscrowApp development sessions
**Skills Integration:** Enhances all existing skills with knowledge-first approach