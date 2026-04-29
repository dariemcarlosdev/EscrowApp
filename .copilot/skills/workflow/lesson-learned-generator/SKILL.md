# Lesson-Learned Generator

Automatically converts mistakes and corrections into portable, reusable prevention rules for AI agents.

## When to Use This Skill

- After experiencing a mistake or requiring user correction
- When detecting repeated patterns of errors across sessions
- When implementing fixes that should become permanent behavioral rules
- When you want to prevent the same type of mistake from happening again

## Core Workflow

### Step 1: Detect Mistake Pattern
Identify what went wrong and why:
- **Code mistakes**: Build failures, test failures, syntax errors
- **Process mistakes**: Missing documentation, incomplete tasks, skipped validation
- **Architecture mistakes**: Dependency violations, pattern misuse
- **User correction patterns**: When user has to fix the same type of issue repeatedly

**Validation checkpoint:** Clearly identify the root cause and the specific trigger condition.

### Step 2: Generate Prevention Rule
Create a specific, actionable rule that would prevent this mistake:
- **Format**: "When [condition], always [action] before [next step]"
- **Scope**: Define if rule applies to this project, all projects, or specific contexts
- **Priority**: Mark as CRITICAL (blocking), HIGH (strongly recommended), or MEDIUM (best practice)

**Validation checkpoint:** Rule should be specific enough to be actionable but general enough to apply to similar situations.

### Step 3: Store Rule in Portable Format
Add rule to the appropriate location:
- **Critical rules**: Add to AGENTS.md as mandatory behavior
- **Project rules**: Create `.github/rules/learned-rules.md`
- **Skill enhancement**: Integrate into existing skill that covers this area
- **Global rules**: Add to NexSynapse rule library

**Validation checkpoint:** Rule is stored where agents will actually encounter it during relevant tasks.

### Step 4: Implement Validation Hook
Create mechanism to detect when rule should trigger:
- **Pre-commit hook**: For code-related rules
- **Session startup check**: For process-related rules
- **Extension monitor**: For real-time validation during work
- **Checklist integration**: For task completion rules

**Validation checkpoint:** Validation mechanism actually prevents the mistake when conditions are met.

### Step 5: Test Rule Effectiveness
Verify the rule prevents the original mistake:
- **Simulate original conditions**: Try to recreate the mistake scenario
- **Verify prevention**: Confirm rule triggers and prevents the error
- **Document success**: Record that rule successfully prevented mistake recurrence

**Validation checkpoint:** Rule demonstrably prevents the original mistake type.

## Example: Documentation Sync Rule

**Mistake Pattern**: Agent completes implementation but forgets to update planning docs
**Generated Rule**: "When marking any task complete, always update both implementation-plan.md and task-checklist.md before calling task_complete"
**Storage**: AGENTS.md mandatory behavior
**Validation**: Check both files modified in same session as task completion

## Constraints

### MUST DO
- Always create specific, actionable rules (not vague guidance)
- Store rules where agents will actually encounter them
- Include validation mechanism to enforce rule compliance
- Test that rule actually prevents the original mistake
- Use consistent rule format for portability across AI platforms

### MUST NOT DO
- Create rules so specific they only apply to one exact scenario
- Generate rules without clear enforcement mechanisms  
- Add rules to locations that agents don't read during relevant work
- Create rules that conflict with existing established patterns
- Generate rules for one-off mistakes that are unlikely to recur