using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Dashboard;

/// <summary>
/// Client (payer) dashboard — shows transactions where the authenticated user
/// is the paying party. Displays active holds, released payments, and disputes.
///
/// PREREQUISITE: User authentication must be implemented before this page
/// is functional. Currently using API key auth only — need user identity
/// to filter transactions by ClientEmail or ClientActorId.
/// </summary>
public sealed partial class ClientDashboard : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IStringLocalizer<ClientDashboard> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private bool _isLoading = true;
    private CancellationTokenSource _cts = new();

    // TODO: Add transaction list property
    // private IReadOnlyList<TransactionSummaryDto>? _transactions;

    protected override async Task OnInitializedAsync()
    {
        // TODO: Implement data loading
        // 1. Get authenticated user identity from AuthState
        // 2. Query transactions via IMediator where ClientEmail matches
        // 3. Calculate summary statistics (active holds, total escrowed, etc.)
        // 4. Set _isLoading = false

        await Task.Delay(0); // placeholder
        _isLoading = false;
    }

    // TODO: Add event handlers
    // private async Task HandleViewDetails(int transactionId) =>
    //     Navigation.NavigateTo($"/dashboard/transaction/{transactionId}");
    //
    // private async Task HandleDispute(int transactionId) { ... }
    // private async Task HandleCancel(int transactionId) { ... }

    public void Dispose() => _cts.Cancel();
}
