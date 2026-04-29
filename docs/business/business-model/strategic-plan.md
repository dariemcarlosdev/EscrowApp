# NexTruzt.io — Strategic Pre-Launch Plan

> Decision-oriented action register for pre-launch readiness.
> Last updated: 2026-04-10

---

## 🔴 Pre-Launch Blockers (GO/NO-GO)

These items MUST be resolved before any public launch. Each has a clear go/no-go gate.

### BLOCKER-1: Fintech Attorney Engagement

| Field | Value |
|-------|-------|
| **Priority** | 🔴 CRITICAL — Blocks everything |
| **Owner** | Founder |
| **Budget** | $5,000 – $20,000 |
| **Target** | Before ANY user-facing marketing or beta launch |
| **Status** | ❌ Not Started |

**Actions:**
1. [ ] Identify 2-3 fintech attorneys with money transmitter / escrow licensing experience
2. [ ] Schedule initial consultation ($500-$1,000 per attorney)
3. [ ] Present the NexTruzt.io business model: Stripe Connect delayed payouts, 1.5% fee, consultant/client marketplace
4. [ ] Get written legal opinion on:
   - Whether NexTruzt.io qualifies as a money transmitter under federal (FinCEN) and state law
   - Whether the "secure payment holding" model requires an escrow license in target states
   - Whether Stripe Connect's platform account model provides regulatory shelter
   - Recommended corporate structure (LLC, money services business registration, etc.)
5. [ ] Receive and file the legal opinion document

**GO/NO-GO Gate:**
- ✅ GO: Attorney confirms Stripe Connect platform model does NOT require escrow license or MTL
- 🟡 CONDITIONAL: Requires registration in specific states → budget $10K-$50K for compliance
- 🔴 NO-GO: Requires full escrow license → pivot business model or find licensed partner

---

### BLOCKER-2: Terminology & Compliance Audit

| Field | Value |
|-------|-------|
| **Priority** | 🔴 CRITICAL — Legal liability |
| **Owner** | Development Team + Attorney |
| **Budget** | Included in attorney retainer |
| **Target** | Immediately after BLOCKER-1 resolution |
| **Status** | ❌ Not Started |

**Actions:**
1. [ ] Scan ALL user-facing text for the word "escrow":
   - `.resx` resource files (SharedResource.resx, SharedResource.es.resx)
   - Component-specific resources (HeroSection, Footer, FaqSection, dashboard)
   - Marketing copy, landing page text
   - Terms of Service, Privacy Policy
   - Email templates, notification text
2. [ ] Replace with attorney-approved alternatives:
   - ❌ "Escrow" → ✅ "Secure Payment Holding" / "Payment Protection" / "Held Funds"
   - ❌ "Escrow Agent" → ✅ "Payment Platform" / "Payment Facilitator"
   - ❌ "Escrow Account" → ✅ "Holding Account" / "Platform Account"
3. [ ] Internal code identifiers (class names, namespaces, DB tables) may retain "escrow" — these are not user-facing
4. [ ] Attorney reviews final user-facing language before launch

**GO/NO-GO Gate:**
- ✅ GO: Zero instances of "escrow" in user-facing content (confirmed by attorney review)
- 🔴 NO-GO: Any unreviewed "escrow" language remains in production UI

---

### BLOCKER-3: Terms of Service & Legal Framework

| Field | Value |
|-------|-------|
| **Priority** | 🔴 CRITICAL — Required for launch |
| **Owner** | Founder + Attorney |
| **Budget** | $2,000 – $5,000 (included in attorney scope) |
| **Target** | Before beta launch |
| **Status** | ❌ Not Started |

**Actions:**
1. [ ] Draft Terms of Service covering:
   - Platform's role as payment facilitator (NOT escrow agent)
   - Funds holding mechanics and release conditions
   - Dispute resolution process and timelines
   - Fee structure transparency (1.5% platform + Stripe fees)
   - Liability limitations
   - Data handling (PCI-DSS compliance via Stripe)
2. [ ] Draft Privacy Policy (GDPR/CCPA compliant)
3. [ ] Attorney review and approval of both documents
4. [ ] Implement ToS acceptance flow in the application

**GO/NO-GO Gate:**
- ✅ GO: Attorney-approved ToS and Privacy Policy live on the platform
- 🔴 NO-GO: Missing or unreviewed legal documents

---

### BLOCKER-4: Stripe Connect Compliance Verification

| Field | Value |
|-------|-------|
| **Priority** | 🟡 HIGH — Technical + Regulatory |
| **Owner** | Development Team |
| **Budget** | $0 (Stripe support is free) |
| **Target** | Before payment processing goes live |
| **Status** | ❌ Not Started |

**Actions:**
1. [ ] Verify Stripe Connect account type (Express vs Custom) meets NexTruzt.io's requirements
2. [ ] Confirm Stripe's platform agreement allows the "delayed payout" model
3. [ ] Address the 7-day manual capture limitation:
   - Current architecture uses `capture_method: manual` (auth expires after 7 days)
   - For projects lasting >7 days: MUST switch to Stripe Connect delayed payouts (capture immediately, hold on platform, transfer on release)
   - Document the architectural migration path
4. [ ] Verify onboarding flow meets Stripe's KYC requirements for connected accounts
5. [ ] Test webhook handling for all payment lifecycle events
6. [ ] Confirm PCI-DSS compliance scope (should be SAQ-A with Stripe handling card data)

**GO/NO-GO Gate:**
- ✅ GO: Stripe confirms platform model is compliant; 7-day limit addressed
- 🔴 NO-GO: Stripe rejects the use case or imposes unworkable restrictions

---

## 🟡 Pre-Launch Priorities (Important but not blocking)

### PRIORITY-1: PayPal as Backup Payment Provider

| Field | Value |
|-------|-------|
| **Priority** | 🟡 MEDIUM |
| **Rationale** | Risk mitigation against Stripe dependency; broader user reach |
| **Architecture Impact** | Low — Strategy Pattern (IFundHoldable/IFundReleasable/IFundCancellable) already supports multiple providers |
| **Effort** | 2-3 sprints after MVP |
| **Status** | 📋 Planned (post-MVP) |

**Actions:**
1. [ ] Research PayPal for Marketplaces (delayed disbursement model)
2. [ ] Implement PayPalPaymentStrategy implementing IEscrowPaymentStrategy
3. [ ] Register via IPaymentStrategyFactory
4. [ ] Update UI to support provider selection

---

### PRIORITY-2: Trust Bootstrapping & Social Proof

| Field | Value |
|-------|-------|
| **Priority** | 🟡 MEDIUM |
| **Rationale** | New fintech platform has zero trust signal; users hesitant to send money |
| **Status** | 📋 Planned |

**Actions:**
1. [ ] Implement "Powered by Stripe" badge and messaging prominently
2. [ ] Create a transparent fee calculator on the landing page
3. [ ] Publish a "How It Works" security explainer (Stripe handles card data, funds are held securely, etc.)
4. [ ] Plan a closed beta with 10-20 known consultants for testimonials
5. [ ] Add a trust seal / security certification badge when available

---

### PRIORITY-3: 7-Day Hold Limit Architecture Migration

| Field | Value |
|-------|-------|
| **Priority** | 🟡 HIGH — Technical |
| **Rationale** | Manual capture expires after 7 days; consulting projects often last weeks/months |
| **Status** | 📋 Planned |

**Architecture Decision:**
- **Current:** Stripe `capture_method: manual` → authorize then capture
- **Target:** Stripe Connect delayed payouts → capture immediately, hold funds on platform account, transfer to consultant on release
- **Impact:** Changes to IFundHoldable implementation, no domain model changes needed
- **Risk:** Must ensure Stripe Connect account is properly configured for delayed payouts

**Actions:**
1. [ ] Design the delayed payout architecture (ADR)
2. [ ] Implement new StripeConnectPaymentStrategy
3. [ ] Add migration path from manual capture to delayed payouts
4. [ ] Update tests for the new payment flow

---

## 💡 Key Strategic Takeaways

### Positioning
- **Fee Arbitrage:** 1.5% vs 20% marketplace commissions = $925 savings on a $5,000 project
- **Express Payout Monetization:** Near-zero marginal cost revenue from expedited payouts
- **Bilateral Protection:** Both consultant AND client protected (vs PayPal's buyer-biased system)
- **Independence:** Consultants keep their client relationships (vs marketplaces that own the relationship)

### Biggest Relaunch Blocker
**GET A FINTECH ATTORNEY INVOLVED EARLY.** This is not optional. The single biggest risk to NexTruzt.io is launching with "escrow" terminology or mechanics that trigger regulatory requirements. An attorney's opinion determines whether the current business model is viable or needs restructuring. Every other pre-launch task is secondary to this.

### Competitive Moat Strategy
1. **Short-term (MVP):** Fee transparency + Stripe trust + faster payouts than competitors
2. **Medium-term (6 months):** Multiple payment providers (PayPal, ACH) + dispute resolution track record
3. **Long-term (12+ months):** Web3/Ethereum bridge for crypto-savvy consultants + smart contract escrow

### Revenue Projection Assumptions
- Year 1: 50 consultants × 24 transactions/year × $3,000 avg = $5.4M GMV → $81K platform revenue
- Year 2: 200 consultants × 30 transactions/year × $4,000 avg = $24M GMV → $360K platform revenue
- Express Payout adoption: 30% of consultants → additional $36K-$108K/year

---

## 📋 Decision Log

| # | Date | Decision | Rationale | Status |
|---|------|----------|-----------|--------|
| D-001 | 2026-04-10 | Engage fintech attorney before any public launch | Escrow licensing risk could invalidate the business model | ❌ Pending |
| D-002 | 2026-04-10 | Remove "escrow" from all user-facing content | Legal liability; term implies licensed escrow agent | ❌ Pending |
| D-003 | 2026-04-10 | Use Stripe Connect as primary payment rail | Best-in-class API, regulatory shelter, brand trust | ✅ Decided |
| D-004 | 2026-04-10 | Charge 1.5% platform fee | Competitive vs 20% marketplaces; sustainable unit economics | ✅ Decided |
| D-005 | 2026-04-10 | Plan PayPal as backup provider post-MVP | Risk mitigation; Strategy Pattern enables zero-impact addition | 📋 Planned |
| D-006 | 2026-04-10 | Migrate from manual capture to Stripe Connect delayed payouts | 7-day authorization limit incompatible with consulting timelines | 📋 Planned |

---

## 🔗 Related Documents

| Document | Purpose |
|----------|---------|
| `business-model.md` | Revenue model, competitive analysis, risk factors |
| `../../architecture/payment-strategies/` | Strategy Pattern technical design |
| `../../audits/security-audit/` | OWASP Top 10 findings |
| `../../cross-cutting/hybrid-identity/` | Identity model for Web2/Web3 bridge |
| `../../planning/implementation-plan.md` | Technical implementation phases |
| `../../planning/task-checklist.md` | Granular task tracking |
