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
public sealed class EscrowController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new escrow transaction and immediately places a payment hold.
    /// </summary>
    [HttpPost("hold")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status201Created)]
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
            request.ProviderName);

        var result = await mediator.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves a single escrow transaction by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTransactionQuery(id), ct);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Lists escrow transactions with pagination and optional status filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<EscrowTransactionResponse>), StatusCodes.Status200OK)]
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
    /// Releases held funds for a transaction. Client confirms service completion.
    /// </summary>
    [HttpPost("{id:int}/release")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Release(
        int id,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ReleaseFundsCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Raises a dispute on a held transaction. Cancels the payment hold.
    /// RaisedBy is derived from the authenticated API client identity.
    /// </summary>
    [HttpPost("{id:int}/dispute")]
    [ProducesResponseType(typeof(EscrowTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Dispute(
        int id,
        [FromBody] DisputeFundsApiRequest request,
        CancellationToken ct)
    {
        // RaisedBy derived from authenticated principal — not from request body (security)
        var raisedBy = User.Identity?.Name ?? "API Client";

        var result = await mediator.Send(
            new DisputeFundsCommand(id, request.Reason, raisedBy), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancels (voids) a held escrow transaction by mutual agreement.
    /// Unlike dispute, cancel is cooperative — both parties agree to void the hold.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(CancelFundsResult), StatusCodes.Status200OK)]
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
