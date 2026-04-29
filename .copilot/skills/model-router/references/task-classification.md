# Task Classification

> 12 task categories for model routing. Each category maps to a dimension weight profile
> that determines which model capabilities matter most.

## Category Definitions

### 1. `code-review`

**Description:** Static analysis of code changes for correctness, style, security, and maintainability.

**Examples:**
- Review staged git changes for a PR
- Check a handler for SOLID violations
- Evaluate a refactored component for regressions

**Key Dimensions:** Code Reading (5), Reasoning (4), Consistency (4)
**Typical Complexity:** M
**Typical Agent Type:** `code-review`, `general-purpose`

---

### 2. `security-audit`

**Description:** Audit code against OWASP Top 10, scan for secrets, evaluate auth flows, and assess injection risks.

**Examples:**
- Scan a payment handler for injection vulnerabilities
- Audit authentication middleware for broken access control
- Check for hardcoded secrets in configuration files

**Key Dimensions:** Reasoning (5), Code Reading (5), Instruction Following (4)
**Typical Complexity:** M–L
**Typical Agent Type:** `security-auditor`, `general-purpose`
**Domain Sensitivity:** Always high — override to `quality-first` budget mode

---

### 3. `test-generation`

**Description:** Generate unit tests, integration tests, or test plans following TDD or AAA patterns.

**Examples:**
- Generate xUnit tests for a MediatR handler
- Create integration tests for the Stripe payment flow
- Write test cases for edge conditions in dispute resolution

**Key Dimensions:** Code Generation (5), Instruction Following (4), Consistency (4)
**Typical Complexity:** M–L
**Typical Agent Type:** `test-engineer`, `general-purpose`

---

### 4. `complex-implementation`

**Description:** Multi-file architectural changes requiring deep reasoning, code generation, and tool coordination.

**Examples:**
- Implement a new payment provider strategy (Strategy pattern)
- Build a complete MediatR vertical slice (command + handler + validator + tests)
- Create a new Blazor component triad with localization and scoped CSS

**Key Dimensions:** Reasoning (5), Code Generation (5), Creative (4), Tools (4)
**Typical Complexity:** L–XL
**Typical Agent Type:** `general-purpose`
**Note:** This is the most demanding category — Premium/Standard models strongly preferred

---

### 5. `refactoring`

**Description:** Code transformation, pattern migration, dependency restructuring without changing behavior.

**Examples:**
- Extract a service from a monolithic handler
- Migrate from role-based to policy-based authorization
- Apply the Strategy pattern to replace conditional logic

**Key Dimensions:** Code Reading (5), Reasoning (4), Code Generation (4), Consistency (4)
**Typical Complexity:** M–L
**Typical Agent Type:** `general-purpose`

---

### 6. `debugging`

**Description:** Root cause analysis, reproducing issues, fixing bugs, and verifying fixes.

**Examples:**
- Diagnose a flaky test in the CI pipeline
- Find the root cause of a null reference exception in payment processing
- Debug a Blazor component lifecycle issue

**Key Dimensions:** Reasoning (5), Code Reading (5), Creative (4), Tools (4)
**Typical Complexity:** M–L
**Typical Agent Type:** `general-purpose`, `rubber-duck`

---

### 7. `documentation`

**Description:** Generate README files, API docs, ADRs, inline comments, or update existing documentation.

**Examples:**
- Generate API documentation for REST endpoints
- Create an ADR for a technology decision
- Update feature docs after implementation changes

**Key Dimensions:** Instruction Following (4), Speed (4), Cost (4)
**Typical Complexity:** S–M
**Typical Agent Type:** `general-purpose`, `explore`

---

### 8. `architecture-review`

**Description:** Evaluate system design, dependency analysis, Clean Architecture compliance, and design patterns.

**Examples:**
- Review the entire solution for dependency direction violations
- Analyze the CQRS implementation for DDD compliance
- Evaluate the event bus architecture for scalability

**Key Dimensions:** Reasoning (5), Context Utilization (5), Creative (4), Consistency (4)
**Typical Complexity:** L–XL
**Typical Agent Type:** `general-purpose`, `rubber-duck`
**Note:** Benefits most from Premium models due to deep reasoning + large context needs

---

### 9. `code-exploration`

**Description:** Codebase navigation, symbol lookup, finding patterns, tracing call chains.

**Examples:**
- Find all implementations of `IEventBus`
- Trace the call chain from UI to database for the hold funds flow
- List all Blazor components that use `IStringLocalizer`

**Key Dimensions:** Speed (5), Cost (5), Tools (4)
**Typical Complexity:** S
**Typical Agent Type:** `explore`
**Note:** Always use Fast/Cheap tier — no reasoning depth needed

---

### 10. `build-test-execution`

**Description:** Running build commands, test suites, linters, and capturing output.

**Examples:**
- Run `dotnet build` and report errors
- Execute `dotnet test` and summarize results
- Run the OWASP security scan tool

**Key Dimensions:** Speed (5), Cost (5), Tools (4)
**Typical Complexity:** S
**Typical Agent Type:** `task`
**Note:** Always use Fast/Cheap tier — minimal reasoning required

---

### 11. `planning-decomposition`

**Description:** Breaking features into tasks, creating sprint plans, writing implementation plans.

**Examples:**
- Decompose a feature spec into vertical slices with dependencies
- Create an implementation plan for a new payment provider
- Plan a migration from Blazor Server to Blazor WASM

**Key Dimensions:** Reasoning (5), Creative (4), Instruction Following (4), Context (4)
**Typical Complexity:** M–L
**Typical Agent Type:** `general-purpose`

---

### 12. `prompt-engineering`

**Description:** Writing, refactoring, and evaluating LLM prompts, system instructions, or skill definitions.

**Examples:**
- Optimize a system prompt for code review accuracy
- Write a new NexSynapse skill SKILL.md
- Evaluate prompt quality using rubrics

**Key Dimensions:** Creative (5), Instruction Following (5), Reasoning (4)
**Typical Complexity:** M
**Typical Agent Type:** `general-purpose`

---

## Classification Decision Tree

```
Is the task about running a command (build, test, lint)?
  → YES: build-test-execution
  → NO: Is it navigating/searching code without modifying it?
    → YES: code-exploration
    → NO: Is it reviewing existing code for issues?
      → YES: Is it focused on security/OWASP?
        → YES: security-audit
        → NO: Is it focused on architecture/design?
          → YES: architecture-review
          → NO: code-review
      → NO: Is it modifying existing code?
        → YES: Is it changing behavior?
          → YES: Is it fixing a bug?
            → YES: debugging
            → NO: complex-implementation
          → NO: refactoring
        → NO: Is it generating new code?
          → YES: Is it generating tests?
            → YES: test-generation
            → NO: complex-implementation
          → NO: Is it generating documentation?
            → YES: documentation
            → NO: Is it planning/decomposing work?
              → YES: planning-decomposition
              → NO: prompt-engineering (or complex-implementation)
```

---

## Ambiguous Task Guidelines

When a task spans multiple categories:

1. **Pick the dominant category** — the one that describes the hardest part of the task
2. **If equal, pick the category with higher Reasoning weight** — erring toward capability is safer
3. **For compound tasks** (e.g., "review and fix"), classify by the write operation (debugging > code-review)
4. **When truly uncertain**, default to `complex-implementation` — it has the broadest weight profile
