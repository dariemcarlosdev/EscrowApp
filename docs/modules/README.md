# Modules

> Product and capability documentation organized by business concern first.

## Module index

- [Authentication](authentication/README.md) - Identity, login, registration, and account architecture
- [Secure payment holding](escrow-payments/README.md) - Payment workflows and fee handling
- [User interface](user-interface/README.md) - Dashboards, landing page, and transaction views
- [System](system/README.md) - Validation, localization, testing, and AI planning

## Placement rules

1. If the document explains a user-visible workflow or a business capability, start in a module.
2. If the document spans multiple modules but is still implementation-facing, prefer `system/`.
3. If the document is truly platform-wide, place it in `..\architecture`, `..\operations`, or `..\business`.
