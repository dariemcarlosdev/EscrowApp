---
description: Build solution and run all tests
---

1. Restore packages
   dotnet restore EscrowApp.sln // turbo

2. Build solution
   dotnet build EscrowApp.sln --no-restore // turbo

3. Run tests
   dotnet test EscrowApp.sln --no-build // turbo

4. Report results
   - On success: "Build succeeded, all tests passed"
   - On failure: Show error details and suggest fixes
