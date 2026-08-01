using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Dashboard;

/// <summary>
/// Transaction detail page — shows full escrow transaction information with
/// status timeline, party details, and context-sensitive action buttons.
///
/// Actions vary by role (Client vs Consultant) and current transaction status:
/// - Client + Held → Release, Dispute, Cancel
/// - Consultant + Held → Request Release, Dispute
/// - Either + Disputed → View dispute details (no actions)
///
/// PREREQUISITE: User authentication + ownership verification.
/// Must verify the authenticated user is a party to this transaction.
/// </summary>
public sealed partial class TransactionDetail : ComponentBase, IDisposable
{
    [Parameter] public int TransactionId { get; set; }

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<TransactionDetail> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool _isLoading = true;
    private bool _notFound;
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(0); // placeholder
        _isLoading = false;
    }

    public void Dispose() => _cts.Cancel();
}
