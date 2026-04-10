# Models — Domain Layer (Pure C#)

- Zero framework dependencies — no EF Core, ASP.NET, or MediatR references
- EscrowTransaction is the aggregate root — all mutations through its methods
- Guard clauses at method entry; throw domain exceptions on invariant violations
- Use record types for value objects (Money, IdempotencyKey)
- Domain events are past-tense facts (PaymentReceivedEvent, DisputeRaisedEvent)
- Status transitions: Pending → Held → Released | Disputed | Cancelled
- Never expose public setters — use behavior methods
