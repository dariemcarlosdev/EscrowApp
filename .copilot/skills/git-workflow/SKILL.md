---
name: git-workflow
description: "Enforce Git branching strategy, conventional commits, semantic versioning, and PR hygiene. Use when committing code, creating branches, or opening pull requests."
---

# Git Workflow

> Maintain clean Git history, atomic commits, and traceable changes through structured branching and commit conventions.

## When to Use

- Creating a new branch for any work
- Committing code changes (every commit)
- Opening a pull request
- Tagging releases or versioning
- Reviewing commit history for debugging or auditing

**When NOT to use:**
- Emergency hotfixes (still follow conventions, just expedite review)
- Generated files (migrations, build artifacts) — these go in .gitignore
- Work-in-progress commits on personal branches (but clean up before PR)

## Core Workflow

### 1. Branch Naming
- ✅ **Checkpoint:** Branch name follows `<type>/<short-description>` convention
- Use prefixes: `feature/`, `fix/`, `chore/`, `docs/`, `refactor/`, `test/`
- Keep descriptions short, kebab-case, descriptive
- Include issue number if applicable: `feature/123-instant-payouts`

**Examples:**
```
feature/stripe-express-payout       ← New feature
fix/dispute-state-transition        ← Bug fix
chore/upgrade-ef-core-9             ← Maintenance
docs/api-integration-guide          ← Documentation
refactor/payment-strategy-isp       ← Code improvement
test/escrow-transaction-coverage    ← Test additions
```

**Anti-patterns:**
```
❌ my-changes
❌ fix
❌ feature-branch
❌ temp
❌ asdf-123
```

### 2. Conventional Commits
- ✅ **Checkpoint:** Every commit message follows conventional commit format
- Format: `<type>(<scope>): <subject>`
- Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `ci`
- Subject: imperative mood, lowercase, no period, <50 chars
- Body (optional): explain WHY, not WHAT (code shows what)
- Footer: breaking changes, issue references, co-authorship

**Template:**
```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

**Examples:**
```
feat(escrow): add Stripe express payout strategy

Implements IFundReleasable with instant_payout flag.
Charges $2 fee passed through to consultant.

Closes #42

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

```
fix(dispute): prevent release of disputed transactions

Previously, disputed transactions could be released if
the handler didn't check Status before calling Stripe.
Added guard clause in ReleaseFundsHandler.

Fixes #89
```

```
refactor(strategies): split IEscrowPaymentStrategy per ISP

Replaces god interface with IFundHoldable, IFundReleasable,
IFundCancellable. No behavior changes.

BREAKING CHANGE: Existing strategy implementations must
implement specific capability interfaces instead of base.
```

### 3. Atomic Commits
- ✅ **Checkpoint:** Each commit represents ONE logical change
- One concept per commit (a feature, a fix, a refactor — not all three)
- Commit compiles and passes tests (no broken intermediate states)
- Related changes go together (test + implementation in same commit)
- Unrelated changes split into separate commits

**Good atomic commits:**
```
Commit 1: feat(escrow): add IdempotencyKey value object
Commit 2: feat(escrow): enforce idempotency in HoldFundsHandler
Commit 3: test(escrow): add idempotency duplicate detection tests
```

**Bad non-atomic commits:**
```
Commit 1: "Fixes and improvements" ← vague, multiple unrelated changes
Commit 2: "WIP" ← not a logical unit
Commit 3: Half a feature ← code doesn't compile
```

### 4. Commit Frequency
- ✅ **Checkpoint:** Commits are frequent, small, and reversible
- Commit after completing a logical unit (passing test, working feature slice)
- NEVER accumulate 500+ lines uncommitted
- Ideal commit size: 50-200 lines (excluding generated code)
- If you can't summarize the commit in <50 chars, it's too big

**Workflow:**
```
1. Write failing test
2. Implement minimum code to pass
3. Run tests — all pass
4. git add -p (review changes interactively)
5. git commit -m "feat(escrow): add X"
6. Repeat for next slice
```

### 5. Pull Request Hygiene
- ✅ **Checkpoint:** PR has clear title, description, linked issue, and acceptance criteria
- **Title:** Same format as commit messages (`feat(escrow): add instant payouts`)
- **Description template:**
  ```markdown
  ## What
  Brief summary of changes

  ## Why
  Business justification or bug impact

  ## How
  High-level implementation approach

  ## Testing
  How to verify this works

  ## Acceptance Criteria
  - [ ] Criterion 1
  - [ ] Criterion 2

  Closes #<issue-number>
  ```
- Link to related issue/spec
- Assign reviewers (at least one for code, one for security if touching payments)
- Add labels: `feature`, `bugfix`, `security`, `docs`

### 6. Semantic Versioning
- ✅ **Checkpoint:** Version tags follow semver (MAJOR.MINOR.PATCH)
- Increment MAJOR for breaking changes (API contract changes)
- Increment MINOR for new features (backward-compatible)
- Increment PATCH for bug fixes (backward-compatible)
- Pre-release tags: `v1.2.0-alpha.1`, `v1.2.0-beta.2`, `v1.2.0-rc.1`
- Tag format: `v1.2.3` (lowercase 'v' prefix)

**Examples:**
```
v0.1.0 → v0.2.0   ← new feature (hold funds workflow)
v0.2.0 → v0.2.1   ← bug fix (dispute state validation)
v0.2.1 → v1.0.0   ← breaking change (strategy interface refactor)
```

### 7. Pre-Commit Checklist
- ✅ **Checkpoint:** All items verified before committing
- [ ] Code compiles (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
- [ ] No debug code or console logs left in
- [ ] No hardcoded secrets or connection strings
- [ ] Commit message follows conventional format
- [ ] Changes are atomic (one logical unit)
- [ ] Related files staged together (test + implementation)

## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "I'll clean up commits later" | You won't. Commit discipline degrades over time. Clean commits NOW save hours during code review and debugging. |
| "The commit message doesn't matter" | Future you will search git log to find when a bug was introduced. Vague messages make that impossible. |
| "I'll squash everything into one commit" | Atomic commits enable git bisect, selective revert, and blame. One giant commit loses all that context. |
| "This is a small change, I don't need a branch" | Small changes break prod just as hard as big ones. Branches enable testing, review, and safe rollback. |
| "WIP commits are fine on my branch" | WIP commits pollute history and make PR review harder. Use `git commit --amend` or interactive rebase. |
| "I'll write the PR description after merge" | The PR description is documentation for why the change exists. Write it when context is fresh. |
| "Semantic versioning is overkill for pre-1.0" | Semver communicates breaking changes even in 0.x. Your users (internal or external) need that signal. |

## Anti-Patterns

| Pattern | Problem | Fix |
|---|---|---|
| **God Commits** | 1500-line commit touching 30 files with message "Updates" | Break into atomic commits per logical change. Use `git add -p` to stage selectively. |
| **Branch Hoarding** | 10+ stale branches that never got merged | Delete branches after merge. Set branch protection rules requiring PR. |
| **Rewriting Public History** | Force-pushing to `main` or shared branches | NEVER force-push to protected branches. Use revert commits instead. |
| **Merge Commit Spam** | Every PR creates a merge commit instead of rebase | Configure "Squash and merge" or "Rebase and merge" in repo settings. |
| **Vague Messages** | "fix bug", "update code", "changes" | Use conventional commits. If you can't name it, you don't understand it. |
| **Missing Issue Links** | PRs don't reference issues or specs | Enable "Require linked issue" in repo settings. PR templates enforce this. |

## Red Flags

Abort and clean up if you observe:

- Commit messages don't explain WHY (only describe WHAT)
- 5+ unrelated changes in a single commit
- Commit history has "fix typo", "fix again", "actually fix" sequences
- Branch names like `temp`, `asdf`, `my-branch`
- PRs with no description or "see commits" as the description
- Force-pushes to `main` or `develop` branches
- Commits include commented-out code blocks
- 10+ file changes with message "misc updates"
- Version tags don't follow semver pattern

## Verification

Before opening a PR:

- [ ] All commits follow conventional commit format
- [ ] Each commit is atomic (one logical change)
- [ ] Commit messages explain WHY, not just WHAT
- [ ] Branch name follows `<type>/<description>` convention
- [ ] No WIP or "fix typo" commits (use interactive rebase to clean)
- [ ] All commits include Co-authored-by trailer if AI-assisted
- [ ] PR title matches conventional commit format
- [ ] PR description includes What, Why, How, Testing, Acceptance Criteria
- [ ] PR links to related issue or spec
- [ ] Version tag follows semver if this is a release
- [ ] No secrets, credentials, or API keys in diff

## Git Patterns for This Project

### Fintech Commit Requirements

Every commit touching payment flows MUST:
- Include test coverage for the change
- Pass OWASP security scan (no new vulnerabilities)
- Preserve audit trail (domain events still fire)
- Not modify amounts or financial calculations without explicit approval

**Payment commit template:**
```
feat(payments): add express payout capability

Implements instant payout via Stripe API with $2 fee.
Idempotency key prevents duplicate charges on retry.
Audit event published after successful capture.

Testing: Added integration test with Stripe test mode.
Security: Fee calculation validated in unit tests.

Closes #123
```

### Branch Protection Rules

Enforce via GitHub settings:
- Require PR before merge to `main`
- Require 1+ approving reviews
- Require status checks (build + tests pass)
- Require up-to-date branches before merge
- No direct commits to `main`
- No force-push to `main`

### Co-Authorship Trailer

All AI-assisted commits MUST include:
```
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Add to every commit where AI generated substantial code (>30% of diff).

## Integration Points

**Before this skill:**
- Code changes staged in working directory

**After this skill:**
- ✅ Clean commit history ready for PR
- ✅ Conventional commits enable automated changelog generation
- ✅ Semantic version tags enable deployment automation

**Chains well with:**
- `incremental-implementation` — commit after each tested slice
- `tdd-coach` — commit after each red-green-refactor cycle
- `code-reviewer` — clean commits make PR review faster
- `ci-cd-builder` — conventional commits trigger correct CI workflows
