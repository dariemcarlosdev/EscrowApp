# GitHub Actions Workflows

CI/CD automation that runs on GitHub-hosted runners.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Platform** | GitHub Actions |
| **Format** | YAML (`.yml`) |
| **Runners** | GitHub-hosted (Ubuntu, Windows, macOS) |
| **Triggers** | `push`, `pull_request`, `schedule`, `workflow_dispatch`, etc. |

## Current Workflows

| Workflow | File | Triggers | Purpose |
|----------|------|----------|---------|
| **CI** | `ci.yml` | push, pull_request | Build, test, and lint the solution on every push and PR |

## Conventions

- **One workflow per concern** — separate CI, deployment, and scheduled tasks into distinct files.
- **YAML format** — use `.yml` extension consistently.
- **Secrets management** — use GitHub Secrets for API keys, connection strings, and credentials. Never hardcode.
- **Caching** — cache NuGet packages (`actions/cache`) to speed up builds.
- **Concurrency** — use `concurrency` groups to cancel redundant runs on the same branch.
- **Naming** — use descriptive `name:` fields for workflows and jobs for clear status checks.

## How to Add a New Workflow

1. Create a `.yml` file in `.github/workflows/`.
2. Define trigger events under `on:`.
3. Define jobs with `runs-on:` and `steps:`.
4. Test by pushing to a feature branch.

```yaml
# .github/workflows/deploy.yml — minimal example
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet publish -c Release
```

## Workflows vs. Extensions vs. Hooks

| Concern | Workflows | Extensions | Git Hooks |
|---------|-----------|------------|-----------|
| **Where** | GitHub servers (cloud) | Local machine (AI session) | Local machine (git event) |
| **When** | After push/PR | During AI-assisted coding | During git operations |
| **Purpose** | CI/CD automation | AI workflow tools | Developer quality gates |

These are complementary layers: hooks catch issues locally, extensions catch issues during AI coding, workflows catch issues in CI after push.

## See Also

- [`ci.yml`](ci.yml) — Current CI pipeline configuration
- [`.github/extensions/build-guardian/`](../extensions/build-guardian/) — Local build verification before commits
- [`.github/hooks/`](../hooks/) — Git hooks for local pre-commit checks
