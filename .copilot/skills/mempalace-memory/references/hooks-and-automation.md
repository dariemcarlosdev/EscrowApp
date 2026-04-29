# Hooks & Automation

> Reference for `mempalace-memory` skill. Load when configuring auto-save, wake-up context, or NexSynapse workflow integration.

---

## MemPalace Hook System

MemPalace supports automatic memory operations at key moments in the AI session lifecycle:

| Hook | Trigger | Action | Purpose |
|------|---------|--------|---------|
| **Auto-save** | Every 15 messages | `mempalace_diary_write` with session summary | Prevent knowledge loss in long sessions |
| **Pre-compact save** | Before `/compact` or context reset | `mempalace_diary_write` + key drawer saves | Survive context window resets |
| **Wake-up** | Session start | `mempalace_wake_up` | Load recent context and relevant memories |
| **Manual save** | Agent decides something is worth remembering | `mempalace_add_drawer` | Intentional knowledge capture |

---

## Wake-Up Protocol

At session start, execute the wake-up sequence:

### Step 1 — Call wake_up

```python
mempalace_wake_up()
```

This returns:
- Recent diary entries (what happened in the last few sessions)
- Any flagged "next steps" from prior sessions
- Relevant memories based on the current working directory / project context

### Step 2 — Contextual Search

Based on the user's first message, search for related memories:

```python
mempalace_search(query="[topic from user's request]", top_k=5)
```

### Step 3 — Load Prior Decisions

If the task involves architecture or implementation choices:

```python
mempalace_search(query="[area] decision", wing="wing_escrowapp", room="room_decisions", top_k=3)
```

### Decision Tree for Wake-Up

```
User says "continue where we left off"
  → mempalace_diary_read(last_n=3)
  → Resume from last diary entry

User describes a new task
  → mempalace_search(query="[task topic]", top_k=5)
  → Check if prior art exists

User asks about something specific
  → mempalace_search(query="[specific topic]", top_k=3)
  → mempalace_kg_query(query="[entity or relationship]")

First session on this project
  → mempalace_browse_palace()
  → Orient yourself on what's stored
```

---

## Auto-Save Patterns

### What to Auto-Save (Every 15 Messages)

The auto-save hook captures a session snapshot:

```python
mempalace_diary_write(
  content="""
  Session progress:
  - Working on: [current task]
  - Completed: [list of completed items]
  - In progress: [current work item]
  - Blocked by: [any blockers]
  - Next steps: [what comes next]
  - Key decisions: [any decisions made this session]
  """
)
```

### What to Save Before /compact

Before a context reset, save with higher detail:

```python
# 1. Save session diary
mempalace_diary_write(
  content="[Detailed session summary with all decisions, findings, and next steps]"
)

# 2. Save any unsaved decisions to drawers
mempalace_add_drawer(
  wing="wing_escrowapp",
  room="room_decisions",
  title="[Decision title]",
  content="[Full decision context]"
)

# 3. Save any debugging insights
mempalace_add_drawer(
  wing="wing_escrowapp",
  room="room_debugging",
  title="[Bug title]",
  content="[Symptom → Root cause → Fix]"
)
```

### Save Priority Matrix

When deciding what to save, prioritize by value:

| Priority | Content Type | Save Method | Why |
|----------|-------------|-------------|-----|
| 🔴 Critical | Architecture decisions, regulatory findings | `mempalace_add_drawer` to specific room | These affect all future work |
| 🟠 High | Bug fixes, security findings | `mempalace_add_drawer` + `mempalace_kg_add` | Prevents duplicate debugging |
| 🟡 Medium | Implementation patterns, workflow insights | `mempalace_add_drawer` | Builds cumulative knowledge |
| 🟢 Low | Session state, progress tracking | `mempalace_diary_write` | Enables session continuity |
| ⚪ Skip | Routine file reads, simple edits, obvious facts | Don't save | Would clutter the palace |

---

## Integration with NexSynapse Superpowers

### systematic-debugging Integration

When the `systematic-debugging` workflow is active:

```
BEFORE investigating:
  mempalace_search(query="[error message or symptom]", wing="wing_escrowapp", room="room_debugging")
  → Check if this bug was seen before

AFTER fixing:
  mempalace_add_drawer(
    wing="wing_escrowapp",
    room="room_debugging",
    title="[Brief bug description]",
    content="Symptom: [what was observed]\nRoot cause: [why it happened]\nFix: [what was changed]\nGuard: [what prevents recurrence]"
  )
  mempalace_kg_add(
    subject="BUG-[short-id]",
    predicate="fixed_in",
    object="[file or handler name]"
  )
```

### executing-plans Integration

When executing implementation plans:

```
BEFORE starting a task:
  mempalace_search(query="[task topic]", wing="wing_escrowapp")
  → Load relevant context and prior decisions

AFTER completing a task:
  mempalace_add_drawer(
    wing="wing_escrowapp",
    room="[appropriate room]",
    title="[What was implemented]",
    content="[Key implementation details, patterns used, decisions made]"
  )
```

### writing-plans Integration

When creating implementation plans:

```
BEFORE planning:
  mempalace_search(query="[feature area] architecture", wing="wing_escrowapp", room="room_decisions")
  mempalace_search(query="[feature area] constraints", wing="wing_escrowapp", room="room_regulatory")
  → Incorporate prior decisions and constraints into the plan

AFTER planning:
  mempalace_add_drawer(
    wing="wing_escrowapp",
    room="room_decisions",
    title="Plan: [feature name]",
    content="[Plan summary, key decisions, approach chosen]"
  )
```

### verification-before-completion Integration

When verifying work before marking done:

```
mempalace_kg_query(query="[component] depends_on")
→ Verify all dependencies are satisfied

mempalace_search(query="[component] requirements", wing="wing_escrowapp")
→ Check stored requirements are met
```

---

## Configuration Checklist

When setting up MemPalace for a new project:

| Step | Action | Verify |
|------|--------|--------|
| 1 | Install MemPalace MCP server | `mempalace_wake_up()` responds |
| 2 | Create project wing | `mempalace_browse_palace()` shows wing |
| 3 | Create topic rooms | `mempalace_browse_room()` works for each room |
| 4 | Seed with key architectural facts | `mempalace_search("architecture")` returns results |
| 5 | Seed KG with core relationships | `mempalace_kg_query("implements")` returns results |
| 6 | Test diary write/read cycle | `mempalace_diary_write()` + `mempalace_diary_read()` roundtrips |

---

## Troubleshooting

| Issue | Likely Cause | Fix |
|-------|-------------|-----|
| `mempalace_wake_up` fails | MCP server not running | Check MCP config, restart server |
| Search returns no results | Wrong wing/room filter, or palace is empty | Broaden search, remove filters, check palace contents |
| KG query returns nothing | Predicates don't match stored triples | Use `mempalace_kg_query` without predicate filter to list all |
| Diary read is empty | No diary entries written yet | Write first entry with `mempalace_diary_write` |
| Too many irrelevant results | Query too broad | Add wing/room filters, reduce top_k, refine query terms |
| AAAK compressed content unclear | Over-compressed | Re-save with plain text for complex decisions |
