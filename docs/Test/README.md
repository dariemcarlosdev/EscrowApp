# Test Runbooks

> Manual verification guides for local and integration-oriented testing that is not fully automated in the repo.

## Available guides

- [Local Stripe CLI webhook test](local-stripe-cli-webhook-test.md) - Step-by-step manual flow for verifying Stripe webhook delivery and transaction correlation against the local EscrowApp runtime.

## Notes

- These guides complement automated tests; they do not replace the existing xUnit coverage.
- Use local test credentials only. Never commit Stripe secrets or webhook signing secrets.
- Payment and webhook flows are compliance-sensitive. Use these guides for development and pre-production verification only.
