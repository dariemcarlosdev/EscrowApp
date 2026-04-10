namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for HeroSection.razor — headline, waitlist form, escrow simulation card.
/// </summary>
public sealed partial class HeroSection
{
    [Inject]
    private IStringLocalizer<HeroSection> L { get; set; } = default!;

    private string WaitlistEmail { get; set; } = string.Empty;
    private bool IsSubmitting { get; set; }
    private bool SubmitSuccess { get; set; }
    private bool SubmitError { get; set; }
    private bool FundsReleased { get; set; }

    private async Task HandleWaitlistSubmit()
    {
        if (string.IsNullOrWhiteSpace(WaitlistEmail))
            return;

        IsSubmitting = true;
        SubmitSuccess = false;
        SubmitError = false;

        try
        {
            await Task.Delay(1200);
            SubmitSuccess = true;
            WaitlistEmail = string.Empty;
        }
        catch
        {
            SubmitError = true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task SimulateRelease()
    {
        if (FundsReleased) return;

        await Task.Delay(800);
        FundsReleased = true;
    }
}
