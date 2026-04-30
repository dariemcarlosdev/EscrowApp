# EscrowApp Documentation

> Canonical entry point for project documentation. Use this index first, then drill into the section README that matches the business or platform concern you are working on.

## Start Here

### Business modules
- [Modules overview](modules/README.md) - Module-first navigation and placement rules
- [Authentication](modules/authentication/README.md) - Login, registration, identity, and auth architecture
- [Secure payment holding](modules/escrow-payments/README.md) - Hold, release, dispute, cancellation, and fee docs
- [User interface](modules/user-interface/README.md) - Dashboards, landing page, and transaction views
- [System](modules/system/README.md) - Validation, localization, testing, and AI roadmap

### Platform references
- [Architecture](architecture/README.md) - System design, patterns, webhooks, and API integration
- [Operations](operations/README.md) - Deployment and production setup
- [Business](business/README.md) - Business model and compliance planning

### Governance and tracking
- [Audits](audits/README.md) - Security review and compliance status
- [Planning](planning/README.md) - Implementation plan, task checklist, and roadmaps
- [Features inventory](features-inventory.md) - Feature implementation status
- [MVP ship checklist](planning/release-readiness/MVP-SHIP-CHECKLIST.md) - Release readiness snapshot

### Support and legacy references
- [Quick fixes](quick-fixes/README.md) - Indexed troubleshooting notes by area
- [Marketing](marketing/README.md) - Repository descriptions and external-facing metadata
- [Legacy cross-cutting docs](cross-cutting/README.md) - Older deep-dive docs kept for reference
- [Legacy index redirect](docs-index.md) - Deprecated entry point retained for old bookmarks

---

## Documentation structure

```text
docs/
|-- modules/        Business and product modules
|-- architecture/   Platform design and technical patterns
|-- operations/     Deployment and runtime guidance
|-- business/       Business model and compliance planning
|-- audits/         Security and compliance review artifacts
|-- planning/       Execution tracking and roadmaps
|-- quick-fixes/    Troubleshooting records
|-- marketing/      External-facing repo copy
`-- cross-cutting/  Legacy references from the pre-module layout
```

## Placement rules

1. Put new feature docs under `modules/<module>/<feature>/`.
2. Put cross-cutting technical guidance under `modules/system/` when it applies across multiple modules.
3. Keep platform-wide architecture, deployment, and business context in the top-level `architecture/`, `operations/`, and `business/` folders.
4. Do not add new primary docs under `cross-cutting/`; treat that folder as legacy reference material.
5. Update the nearest section README whenever you add a new document.

## Why this structure

- **Module-first discovery** keeps related product knowledge together.
- **Section README files** make every major folder browsable on its own.
- **Legacy guidance** preserves older documents without letting them compete with the current structure.

**Last updated:** 2026-04-29
