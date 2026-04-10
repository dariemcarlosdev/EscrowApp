using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Dashboard;

/// <summary>
/// Consultant (payee) dashboard — shows transactions where the authenticated user
/// is the service provider. Displays secured funds, earnings, and delivery status.
///
/// PREREQUISITE: User authentication must be implemented. Need user identity
/// to filter transactions by ConsultantEmail or ConsultantActorId.
/// </summary>
public sealed partial class ConsultantDashboard : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<ConsultantDashboard> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool _isLoading = true;
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        // TODO: Implement data loading
        // 1. Get authenticated user identity from AuthState
        // 2. Query transactions via IMediator where ConsultantEmail matches
        // 3. Calculate earnings summary (secured, earned, pending delivery)
        // 4. Set _isLoading = false

        await Task.Delay(0); // placeholder
        _isLoading = false;
    }

    // TODO: Add event handlers
    // private async Task HandleViewDetails(int transactionId) =>
    //     Navigation.NavigateTo($"/dashboard/transaction/{transactionId}");
    //
    // private async Task HandleMarkDelivered(int transactionId) { ... }
    // private async Task HandleRequestRelease(int transactionId) { ... }

    public void Dispose() => _cts.Cancel();
}
