---
description: Write and run tests using TDD red-green-refactor cycle with xUnit and FluentAssertions
---

Invoke test-generator and tdd-coach skills. Read and follow:

```
cat .github/skills/testing/test-generator/SKILL.md
cat .github/skills/testing/tdd-coach/SKILL.md
```

For the current code or feature:

1. Identify what needs testing — handlers, domain models, validators, API endpoints
2. Write failing tests first (RED) using xUnit + FluentAssertions
3. Follow Arrange-Act-Assert pattern with descriptive names: `MethodName_Scenario_ExpectedResult`
4. Run `dotnet test` to confirm tests fail for the right reason
5. Implement or fix code to make tests pass (GREEN)
6. Refactor if needed while keeping tests green
7. Check coverage gaps with test-coverage-analyzer:
   ```
   cat .github/skills/testing/test-coverage-analyzer/SKILL.md
   ```
