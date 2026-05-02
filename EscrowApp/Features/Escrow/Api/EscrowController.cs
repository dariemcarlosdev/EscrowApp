using EscrowApp.Features.Escrow.Api;
using EscrowApp.Features.Escrow.CancelFunds;
using EscrowApp.Features.Escrow.CreateAndHoldFunds;
using EscrowApp.Features.Escrow.DisputeFunds;
using EscrowApp.Features.Escrow.GetTransaction;
using EscrowApp.Features.Escrow.ListTransactions;
using EscrowApp.Features.Escrow.ReleaseFunds;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EscrowApp.Features.Escrow.Api;

/// <summary>
/// REST API for third-party escrow integration.
/// All endpoints require API Key authentication via X-Api-Key header.
/// </summary>
[ApiController]
[Route("api/escrow")]
[Authorize(Policy = "ApiAccess")]
[Produces("application/json")]
// Opt out of Blazor's global antiforgery middleware — this is a stateless API authenticated via
// X-Api-Key header, not a cookie-bearing browser context, so CSRF protection does not apply.
// Without this the AntiforgeryMiddleware (registered via app.UseAntiforgery()) rejects all
// POST/PUT/PATCH/DELETE with "The request has an incorrect Content-type." (HTTP 400).
[IgnoreAntiforgeryToken]
public sealed class EscrowController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new secure-payment-holding transaction and atomically authorizes a hold
    /// against the supplied payment method via the configured payment provider (default: Stripe).
    /// </summary>
    /// <remarks>
    /// Behavior:
    /// <list type="bullet">
    ///   <item><description>Persists a transaction in <c>Pending</c> state, then calls the provider's
    ///   <c>IFundHoldable</c> strategy to authorize (manual capture) the funds.</description></item>
    ///   <item><description>On success the transaction is updated to <c>Held</c> and the provider
    ///   reference (e.g. Stripe PaymentIntent ID) is stored in <c>ExternalReference</c>.</description></item>
    ///   <item><description>The supplied <c>X-Idempotency-Key</c> is forwarded to the provider so that
    ///   safe retries do not create duplicate holds. If omitted, a server-generated GUID is used —
    ///   clients that need retry safety MUST send the header.</description></item>
    ///   <item><description>A <c>PaymentReceivedEvent</c> is published on the in-process event bus
    ///   after persistence succeeds.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">Payer/payee emails, amount, service description, payment method ID,
    /// and optional provider name (defaults to <c>Stripe</c>).</param>
    /// <param name="idempotencyKey">Optional <c>X-Idempotency-Key</c> header. Required for at-least-once
    /// safety against client/network retries. Auto-generated if absent.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>201 Created with the persisted transaction and a <c>Location</c> header pointing to
    /// <see cref="GetById"/>.</returns>
    /// <response code="201">Transaction created and funds successfully held.</response>
    /// <response code="400">Validation failure (missing fields, invalid email, non-positive amount).</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    /// <response code="422">Provider declined the authorization (insufficient funds, card error, dispute).</response>
    [HttpPost("hold")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAndHold(
        [FromBody] CreateAndHoldRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var command = new CreateAndHoldFundsCommand(
            request.ClientEmail,
            request.ConsultantEmail,
            request.Amount,
            request.ServiceDescription,
            request.PaymentMethodId,
            idempotencyKey ?? Guid.NewGuid().ToString(),
            request.ProviderName);

        var result = await mediator.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves a single secure-payment-holding transaction by its database identifier.
    /// </summary>
    /// <remarks>
    /// Read-only projection served via <c>GetTransactionQuery</c> (MediatR). Uses
    /// <c>AsNoTracking()</c> on the repository for performance and does not load related
    /// audit/event records. The returned <c>Status</c> reflects the latest persisted state
    /// (<c>Pending</c> | <c>Held</c> | <c>Released</c> | <c>Disputed</c> | <c>Cancelled</c>).
    /// </remarks>
    /// <param name="id">Internal transaction ID returned by <see cref="CreateAndHold"/>.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>200 OK with the transaction, or 404 if no transaction exists with that ID.</returns>
    /// <response code="200">Transaction found.</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    /// <response code="404">No transaction exists for the supplied ID.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTransactionQuery(id), ct);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Lists secure-payment-holding transactions with pagination and an optional status filter.
    /// </summary>
    /// <remarks>
    /// Pagination is 1-based. The handler clamps invalid input defensively, but callers should
    /// send <c>page &gt;= 1</c> and a reasonable <c>pageSize</c> (recommended &lt;= 100). The
    /// <c>status</c> filter, when supplied, must match a known status string exactly
    /// (<c>Pending</c>, <c>Held</c>, <c>Released</c>, <c>Disputed</c>, <c>Cancelled</c>);
    /// unknown values yield an empty page rather than an error.
    /// </remarks>
    /// <param name="page">1-based page number. Defaults to <c>1</c>.</param>
    /// <param name="pageSize">Items per page. Defaults to <c>20</c>.</param>
    /// <param name="status">Optional exact-match status filter.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>200 OK with a paginated envelope (items + total count + page metadata).</returns>
    /// <response code="200">Page returned (may be empty).</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<EscrowTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new ListTransactionsQuery(page, pageSize, status), ct);
        return Ok(result);
    }

    /// <summary>
    /// Captures (releases) previously held funds, transferring them to the consultant payee.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// <list type="bullet">
    ///   <item><description>Transaction must be in <c>Held</c> state. Releasing a <c>Pending</c>,
    ///   <c>Released</c>, <c>Disputed</c>, or <c>Cancelled</c> transaction yields <c>422</c>.</description></item>
    ///   <item><description>The provider's <c>IFundReleasable</c> strategy is invoked using the
    ///   stored <c>ExternalReference</c> (e.g. Stripe PaymentIntent capture).</description></item>
    /// </list>
    /// On success the transaction transitions to <c>Released</c>, the platform fee is recorded,
    /// and a release domain event is published after persistence. Idempotency is enforced via
    /// the <c>X-Idempotency-Key</c> header — repeating the same call with the same key returns
    /// the same result without double-capturing.
    /// </remarks>
    /// <param name="id">Internal transaction ID.</param>
    /// <param name="idempotencyKey">Optional <c>X-Idempotency-Key</c> header for safe retries.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>200 OK with the release result (final amount, fee, provider reference).</returns>
    /// <response code="200">Funds successfully captured.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    /// <response code="404">Transaction not found.</response>
    /// <response code="422">Transaction is not in a releasable state (e.g. already released, disputed, or cancelled).</response>
    [HttpPost("{id:int}/release")]
    [ProducesResponseType(typeof(ReleaseFundsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Release(
        int id,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReleaseFundsCommand(id, idempotencyKey ?? Guid.NewGuid().ToString()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Raises a dispute against a held transaction and voids the underlying payment authorization.
    /// </summary>
    /// <remarks>
    /// Behavior:
    /// <list type="bullet">
    ///   <item><description>Transaction must be in <c>Held</c> state. Disputes against terminal
    ///   states (<c>Released</c>, <c>Cancelled</c>, already <c>Disputed</c>) return <c>422</c>.</description></item>
    ///   <item><description>The provider's <c>IFundCancellable</c> strategy voids the hold so funds
    ///   are returned to the payer's instrument.</description></item>
    ///   <item><description>Status transitions to <c>Disputed</c>, <c>DisputeReason</c> is persisted,
    ///   and a <c>DisputeRaisedEvent</c> is published.</description></item>
    /// </list>
    /// Security: <c>RaisedBy</c> is derived from <c>User.Identity.Name</c> (authenticated API
    /// client), <b>not</b> from the request body — this prevents spoofing of the dispute origin
    /// in the audit trail.
    /// </remarks>
    /// <param name="id">Internal transaction ID.</param>
    /// <param name="request">Dispute payload — only <c>Reason</c> is consumed; any caller-supplied
    /// identity field is ignored by design.</param>
    /// <param name="idempotencyKey">Optional <c>X-Idempotency-Key</c> header for safe retries.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>200 OK with the dispute result.</returns>
    /// <response code="200">Dispute recorded and hold voided.</response>
    /// <response code="400">Validation failure (missing reason).</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    /// <response code="404">Transaction not found.</response>
    /// <response code="422">Transaction is not in a disputable state.</response>
    [HttpPost("{id:int}/dispute")]
    [ProducesResponseType(typeof(DisputeFundsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Dispute(
        int id,
        [FromBody] DisputeFundsApiRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // RaisedBy derived from authenticated principal — not from request body (security)
        var raisedBy = User.Identity?.Name ?? "API Client";

        var result = await mediator.Send(
            new DisputeFundsCommand(id, request.Reason, raisedBy, idempotencyKey ?? Guid.NewGuid().ToString()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cooperatively voids a held transaction by mutual agreement of payer and payee.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cancel is the non-adversarial counterpart to <see cref="Dispute"/>: both flows ultimately
    /// void the provider hold via <c>IFundCancellable</c>, but they emit distinct domain events
    /// and yield distinct terminal statuses (<c>Cancelled</c> vs. <c>Disputed</c>) so the audit
    /// trail and downstream reporting can distinguish cooperative voids from contested ones.
    /// </para>
    /// <para>
    /// Preconditions: transaction must be in <c>Held</c> state. <c>CancelledBy</c> is derived from
    /// the authenticated principal (not the request body) for the same anti-spoofing reason as
    /// <see cref="Dispute"/>.
    /// </para>
    /// </remarks>
    /// <param name="id">Internal transaction ID.</param>
    /// <param name="request">Cancel payload — only <c>Reason</c> is consumed.</param>
    /// <param name="idempotencyKey">Optional <c>X-Idempotency-Key</c> header for safe retries.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>200 OK with the cancel result.</returns>
    /// <response code="200">Hold voided and transaction marked <c>Cancelled</c>.</response>
    /// <response code="401">Missing or invalid <c>X-Api-Key</c>.</response>
    /// <response code="404">Transaction not found.</response>
    /// <response code="422">Transaction is not in a cancellable state.</response>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(CancelFundsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelFundsApiRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var cancelledBy = User.Identity?.Name ?? "API Client";

        var result = await mediator.Send(
            new CancelFundsCommand(id, request.Reason, cancelledBy, idempotencyKey ?? Guid.NewGuid().ToString()), ct);
        return Ok(result);
    }
}
