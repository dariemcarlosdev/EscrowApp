---
name: incremental-implementation
description: "Build features in thin vertical slices with test-first discipline. Use when implementing any feature to prevent big-bang integration failures and enable continuous delivery."
---

# Incremental Implementation

> Ship working software in testable vertical slices — one tested feature increment at a time.

## When to Use

- Implementing any feature (large or small)
- Converting a spec or task list into working code
- Building a new MediatR handler, Blazor component, or strategy implementation
- Any time you're tempted to write 500+ lines before testing

**When NOT to use:**
- Exploratory prototyping (throwaway code) — but even prototypes benefit from slicing
- Emergency hotfixes (still test, just compress the cycle)
- Refactoring existing code without adding features (use smart-refactor instead)

## Core Workflow

### 1. Pick ONE Slice
- ✅ **Checkpoint:** Single smallest deliverable task selected from plan
- Review task checklist or spec — pick the SMALLEST completable unit
- Prefer vertical slices (UI → Handler → Repository → DB) over horizontal layers
- Slice should be demonstrable: "user can now do X"
- If the task feels too big, split it further

**Example Slices (NexTruzt.io):**
```
❌ TOO BIG: "Implement escrow workflow"

✅ RIGHT SIZE:
  Slice 1: Create EscrowTransaction entity with status field
  Slice 2: Add HoldFundsCommand with validation
  Slice 3: Implement HoldFundsHandler (happy path only)
  Slice 4: Wire Stripe manual capture strategy
  Slice 5: Add error handling for insufficient funds
  Slice 6: Build UI component for hold funds button
```

### 2. Load Context
- ✅ **Checkpoint:** Existing patterns identified, ready to match conventions
- Find similar existing code: `grep -r "IRequestHandler" Features/Escrow/`
- Review architecture docs for the layer you're working in
- Check AGENTS.md, CLAUDE.md, or GEMINI.md for conventions
- Verify API signatures in official docs (link to source-driven-development)

**Context checklist:**
- [ ] Found 1-2 examples of similar code (MediatR handlers, Blazor components, etc.)
- [ ] Reviewed naming conventions (Commands end in `Command`, handlers in `Handler`)
- [ ] Checked DI registration pattern in Program.cs
- [ ] Verified code-behind pattern for Blazor (3 files: .razor, .razor.cs, .razor.css)
- [ ] Read relevant feature doc in `docs/features/`

### 3. Write Failing Test FIRST
- ✅ **Checkpoint:** Red test written for expected behavior
- Follow TDD: Red → Green → Refactor (link to tdd-coach)
- Test file lives in `EscrowApp.Tests/` mirroring source structure
- Use Arrange-Act-Assert pattern
- Test method name: `MethodName_Scenario_ExpectedResult`
- Use FluentAssertions for readable assertions

**Example (xUnit + FluentAssertions):**
```csharp
// EscrowApp.Tests/Features/Escrow/HoldFunds/HoldFundsHandlerTests.cs
[Fact]
public async Task Handle_ValidTransaction_TransitionsToHeldStatus()
{
    // Arrange
    var transaction = new EscrowTransactionBuilder()
        .WithStatus(EscrowStatus.Created)
        .WithAmount(100m)
        .Build();
    
    var mockRepo = new Mock<IEscrowTransactionRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(transaction);
    
    var handler = new HoldFundsHandler(mockRepo.Object);
    var command = new HoldFundsCommand(transaction.Id);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    transaction.Status.Should().Be(EscrowStatus.Held);
}
```

Run test — it MUST fail (red):
```
dotnet test --filter "FullyQualifiedName~HoldFundsHandlerTests"
```

### 4. Implement MINIMUM Code
- ✅ **Checkpoint:** Simplest implementation that passes the test
- Write ONLY enough code to make the test pass
- No premature optimization, no gold-plating
- Match existing patterns (vertical slice structure, DI, naming)
- Follow Clean Architecture layer rules (no EF Core in domain)

**Minimum implementation:**
```csharp
// Features/Escrow/HoldFunds/HoldFundsHandler.cs
public sealed class HoldFundsHandler(
    IEscrowTransactionRepository repository) 
    : IRequestHandler<HoldFundsCommand, HoldFundsResult>
{
    public async Task<HoldFundsResult> Handle(
        HoldFundsCommand command, 
        CancellationToken ct)
    {
        var transaction = await repository.GetByIdAsync(command.TransactionId, ct);
        if (transaction is null)
            return HoldFundsResult.Failure("Transaction not found");

        transaction.HoldFunds(); // Domain method
        await repository.UpdateAsync(transaction, ct);

        return HoldFundsResult.Success();
    }
}
```

**DON'T implement yet:**
- Error handling for edge cases (next slice)
- Logging (next slice)
- Stripe API integration (next slice)
- UI validation (next slice)

### 5. Run Full Test Suite
- ✅ **Checkpoint:** New test passes, no regressions
- Run ALL tests, not just the new one
- Zero failures — if existing tests broke, fix before proceeding
- Check test coverage if available (but don't obsess over %)

```powershell
dotnet test
```

Expected output:
```
Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47
```

If failures:
- Fix the regression immediately
- If the test was wrong, update it
- Don't proceed until all tests green

### 6. Build and Verify Compilation
- ✅ **Checkpoint:** Solution builds without errors or warnings
- Zero warnings — treat warnings as errors in this project
- Check for nullability warnings, unused usings, missing XML docs

```powershell
dotnet build /p:TreatWarningsAsErrors=true
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 7. Commit with Descriptive Message
- ✅ **Checkpoint:** Atomic commit following conventional commits
- Follow git-workflow skill conventions
- Commit message format: `<type>(<scope>): <subject>`
- Include Co-authored-by trailer if AI-assisted

```bash
git add Features/Escrow/HoldFunds/HoldFundsHandler.cs
git add Features/Escrow/HoldFunds/HoldFundsCommand.cs
git add Features/Escrow/HoldFunds/HoldFundsResult.cs
git add EscrowApp.Tests/Features/Escrow/HoldFunds/HoldFundsHandlerTests.cs

git commit -m "feat(escrow): add HoldFundsHandler with basic validation

Implements IRequestHandler for HoldFundsCommand.
Validates transaction existence and delegates to domain.
Happy path only — error handling in next slice.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

### 8. Mark Task Complete and Move to Next
- ✅ **Checkpoint:** Progress tracked, ready for next slice
- Update task checklist: `[x]` the completed item
- Update planning docs (implementation-plan.md) if phase complete
- Pick next slice from the list
- REPEAT workflow from step 1

**DO NOT:**
- Start the next slice without committing
- Accumulate uncommitted work across multiple slices
- Skip updating the task checklist

## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "I'll test everything at the end" | End-to-end testing catches bugs when they're most expensive to fix. Test each slice as you build. |
| "This is too small to commit" | Small commits are GOOD. They're easy to review, revert, bisect, and cherry-pick. |
| "Let me build the whole feature first" | You're building risk, not value. A 1000-line uncommitted diff is a failure mode. |
| "I need to refactor before I can add this" | Implement first, refactor after with green tests. Working code beats perfect architecture. |
| "Writing tests slows me down" | Writing tests FIRST catches bugs in minutes instead of hours. Test-driven is faster. |
| "I can't slice this any smaller" | You can. Split UI from API. Split happy path from error handling. Split read from write. |
| "I'll add tests later" | You won't. Untested code ships broken and stays broken. Test NOW. |

## Anti-Patterns

| Pattern | Problem | Fix |
|---|---|---|
| **Big Bang Integration** | Build entire feature without testing, integrate at the end → 40 errors | Slice into 5 increments, test each one. Catch errors when they're isolated. |
| **Horizontal Layer Completion** | Build all entities, then all repos, then all handlers → nothing works end-to-end | Build vertical slices: one entity + one repo + one handler at a time. |
| **Test Procrastination** | Write 500 lines, then write tests → tests find 12 bugs | Test-first: 10 lines code → 1 test → pass → commit. Repeat. |
| **Scope Creep Mid-Slice** | "While I'm here, let me also add…" → slice balloons to 800 lines | Finish the slice. Commit. THEN add the extra feature as a new slice. |
| **Uncommitted Pile-Up** | 5 features implemented, nothing committed → git diff is 2000 lines | Commit after EACH slice. Max uncommitted diff: 200 lines. |
| **Gold Plating** | Implement error handling, logging, retries, caching for MVP | Implement ONLY what the current test requires. Add enhancements in later slices. |

## Red Flags

Abort and re-slice if you observe:

- Uncommitted diff exceeds 300 lines
- 30+ minutes elapsed without a green test
- Implementing code not covered by a failing test
- Modifying 5+ files for "one feature"
- Can't explain what you're building in one sentence
- Breaking existing tests and "will fix later"
- Adding dependencies (NuGet packages) mid-slice without verification
- Implementing "nice to have" features before "must have" is done

## Verification

After each slice, confirm:

- [ ] ONE task from checklist implemented (vertical slice)
- [ ] Test written BEFORE implementation (TDD)
- [ ] Test was red, now green
- [ ] Full test suite passes (no regressions)
- [ ] Code compiles with zero warnings
- [ ] Code matches existing patterns (vertical slice, Clean Architecture, code-behind)
- [ ] Atomic commit made with conventional message
- [ ] Task marked complete in checklist
- [ ] Uncommitted changes < 50 lines (next slice prep only)

## Slice Sizing Guidelines

| Type | Lines of Code | Test Count | Commit Frequency |
|---|---|---|---|
| **Ideal Slice** | 50-150 | 1-3 | Every 15-30 min |
| **Acceptable** | 150-300 | 3-5 | Every 30-60 min |
| **Too Large** | 300+ | 5+ | Split into 2+ slices |

**If your slice is >300 LOC, split it:**
- Separate happy path from error handling
- Separate domain logic from infrastructure
- Separate command from query
- Separate UI from API

## Domain-Specific Slicing (NexTruzt.io)

### MediatR Handler Slices
1. Command + Validator (with tests)
2. Handler happy path (with mock repository)
3. Strategy integration (with mock Stripe)
4. Error handling (with failure tests)
5. Event publishing (with event bus verification)
6. Integration test (WebApplicationFactory)

### Blazor Component Slices
1. .razor markup (static HTML)
2. .razor.cs code-behind (with IMediator injection)
3. .razor.css scoped styles
4. OnInitializedAsync data loading
5. EventCallback wiring for parent communication
6. Localization (IStringLocalizer)

### Payment Strategy Slices
1. Interface definition (IFundHoldable)
2. Stripe implementation (mock SDK)
3. Idempotency key handling
4. Error mapping (Stripe → domain exceptions)
5. Integration test (Stripe test mode)

## Integration Points

**Before this skill:**
- Spec or task list exists
- You know WHAT to build

**After this skill:**
- ✅ Feature complete, tested, committed
- ✅ Ready for code review (code-reviewer skill)
- ✅ Ready for security scan (owasp-audit skill)
- ✅ Ready for deployment (deployment-preflight skill)

**Chains well with:**
- `tdd-coach` — guides the red-green-refactor cycle for each slice
- `source-driven-development` — validates API usage before implementing
- `git-workflow` — ensures clean commit history
- `code-reviewer` — PR review after all slices complete
- `mvp-gatekeeper` — prevents gold-plating mid-slice
