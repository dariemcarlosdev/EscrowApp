using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Dashboard;

/// <summary>
/// Client (payer) dashboard — shows transactions where the authenticated user
/// is the paying party. Displays active holds, released payments, and disputes.
/// </summary>
public sealed partial class ClientDashboard : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<ClientDashboard> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool _isLoading = true;
    private bool _hasTransactions = false;
    private int _heldCount = 0;
    private int _releasedCount = 0;
    private int _disputedCount = 0;
    private decimal _totalAmount = 0m;
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        // TODO: wire to ListTransactionsQuery filtered by authenticated user email
        await Task.Delay(0);
        _isLoading = false;
    }

    private void HandleNewEscrow()
    {
        // TODO: navigate to new escrow creation wizard
        Navigation.NavigateTo("/dashboard/client/new");
    }

    public void Dispose() => _cts.Cancel();
}
