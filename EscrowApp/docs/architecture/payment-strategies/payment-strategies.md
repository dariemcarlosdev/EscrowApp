# 04 — Payment Strategies

> Strategy Pattern + Interface Segregation for multi-provider payment support.
> Add new providers (PayPal, Ethereum) without modifying existing code.

## Status: Implemented (Stripe); Extensible for additional providers

---

## Overview

The escrow platform is designed to support **multiple payment providers** — each with
different capabilities. Stripe supports auth-and-capture holds. Crypto wallets may
support immediate settlement only. The Strategy Pattern combined with ISP (Interface
Segregation Principle) ensures each provider only implements the capabilities it supports.

## Interface Hierarchy

```
                    IEscrowPaymentStrategy (marker)
                    ├── ProviderName: string
                    │
        ┌───────────┼──────────────┐
        ▼           ▼              ▼
  IFundHoldable  IFundReleasable  IFundCancellable
  (auth hold)    (capture/release) (cancel/refund)
```

### Marker Interface

```csharp
// File: Services/Strategies/IEscrowPaymentStrategy.cs
public interface IEscrowPaymentStrategy
{
    string ProviderName { get; }
}
```

### Capability Interfaces (ISP)

```csharp
// File: Services/Strategies/IFundHoldable.cs
public interface IFundHoldable
{
    Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId, string idempotencyKey);
}

// File: Services/Strategies/IFundReleasable.cs
public interface IFundReleasable
{
    Task<bool> ReleaseFundsAsync(string externalReference, string idempotencyKey);
}

// File: Services/Strategies/IFundCancellable.cs
public interface IFundCancellable
{
    Task<bool> CancelHoldAsync(string externalReference, string idempotencyKey);
}
```

> **Why ISP?** Not all providers support all capabilities. ACH providers may not support
> holds. Crypto wallets may not support cancellation. Each provider implements only the
> interfaces matching its capabilities.

## Factory

```csharp
// File: Services/Strategies/PaymentStrategyFactory.cs
public sealed class PaymentStrategyFactory : IPaymentStrategyFactory
{
    // Injected: IEnumerable<IEscrowPaymentStrategy> strategies

    public IFundHoldable   ResolveHoldStrategy(string providerName);
    public IFundReleasable ResolveReleaseStrategy(string providerName);
    public IFundCancellable ResolveCancelStrategy(string providerName);
}
```

**Resolution logic:**

1. Find strategy where `ProviderName` matches (case-sensitive)
2. If not found → `InvalidOperationException("No payment strategy registered for '{name}'")`
3. Cast to requested capability interface
4. If provider doesn't implement that capability → `NotSupportedException`

## Current Implementation: Stripe

```csharp
// File: Services/Strategies/StripePaymentStrategy.cs
public sealed class StripePaymentStrategy
    : IEscrowPaymentStrategy, IFundHoldable, IFundReleasable, IFundCancellable
{
    public string ProviderName => "Stripe";
}
```

Stripe implements **all three** capabilities because it supports auth-and-capture:

| Capability       | Stripe API Call                     | Returns                      |
| ---------------- | ----------------------------------- | ---------------------------- |
| `HoldFundsAsync`    | `PaymentIntentService.CreateAsync` (manual capture) | PaymentIntent ID  |
| `ReleaseFundsAsync` | `PaymentIntentService.CaptureAsync`                 | `true` if succeeded |
| `CancelHoldAsync`   | `PaymentIntentService.CancelAsync`                  | `true` if canceled  |

## DI Registration

```csharp
// Program.cs
builder.Services.AddScoped<IEscrowPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
```

The factory receives `IEnumerable<IEscrowPaymentStrategy>` via constructor injection —
all registered strategies are automatically available.

## How to Add a New Provider

### Step 1: Create the Strategy Class

```csharp
// Services/Strategies/PayPalPaymentStrategy.cs
public sealed class PayPalPaymentStrategy
    : IEscrowPaymentStrategy, IFundHoldable, IFundReleasable
{
    public string ProviderName => "PayPal";

    public async Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId, string idempotencyKey)
    {
        // PayPal Orders API — authorize payment
    }

    public async Task<bool> ReleaseFundsAsync(string externalReference, string idempotencyKey)
    {
        // PayPal capture authorized payment
    }
    // Note: PayPal may not implement IFundCancellable if void is not supported
}
```

### Step 2: Register in DI

```csharp
// Program.cs — add one line
builder.Services.AddScoped<IEscrowPaymentStrategy, PayPalPaymentStrategy>();
```

### Step 3: Use It

```csharp
await Mediator.Send(new HoldFundsCommand(transactionId, paymentMethodId, "PayPal"));
```

**That's it.** No changes to:
- `PaymentStrategyFactory` (discovers via DI)
- `HoldFundsHandler` (resolves by name)
- `ReleaseFundsHandler` (reads stored provider)
- `DisputeFundsHandler` (reads stored provider)

This is the **Open/Closed Principle (OCP)** in action.

## Source Files

| File                                           | Responsibility                              |
| ---------------------------------------------- | ------------------------------------------- |
| `Services/Strategies/IEscrowPaymentStrategy.cs`| Marker interface with ProviderName          |
| `Services/Strategies/IFundHoldable.cs`         | Hold capability (auth-only)                 |
| `Services/Strategies/IFundReleasable.cs`       | Release capability (capture)                |
| `Services/Strategies/IFundCancellable.cs`      | Cancel capability (void/refund)             |
| `Services/Strategies/IPaymentStrategyFactory.cs`| Factory interface                          |
| `Services/Strategies/PaymentStrategyFactory.cs`| Runtime strategy resolution                 |
| `Services/Strategies/StripePaymentStrategy.cs` | Stripe SDK implementation                   |

## Planned Providers

| Provider   | Hold | Release | Cancel | Notes                              |
| ---------- | ---- | ------- | ------ | ---------------------------------- |
| Stripe     | ✅   | ✅      | ✅     | Implemented — manual capture flow  |
| PayPal     | 🔜   | 🔜      | 🔜     | Planned — Orders API               |
| Ethereum   | 🔜   | 🔜      | ❌     | Planned — smart contract escrow    |
