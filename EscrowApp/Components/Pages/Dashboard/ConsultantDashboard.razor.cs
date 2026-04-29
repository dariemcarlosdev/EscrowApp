using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Dashboard;

/// <summary>
/// Consultant (payee) dashboard — shows transactions where the authenticated user
/// is the service provider. Displays secured funds, earnings, and delivery status.
/// </summary>
public sealed partial class ConsultantDashboard : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<ConsultantDashboard> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool _isLoading = true;
    private bool _hasEngagements = false;
    private decimal _fundsSecured = 0m;
    private decimal _totalEarned = 0m;
    private int _pendingCount = 0;
    private int _disputedCount = 0;
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        // TODO: wire to ListTransactionsQuery filtered by authenticated consultant email
        await Task.Delay(0);
        _isLoading = false;
    }

    public void Dispose() => _cts.Cancel();
}
