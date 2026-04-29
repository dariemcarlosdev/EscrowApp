using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EscrowApp.Models;
using EscrowApp.Data;

namespace EscrowApp.Features.Auth.Register;

/// <summary>
/// Handler for RegisterCommand — creates ApplicationUser + Actor bridge via UserManager
/// and assigns the requested role (Client or Consultant) atomically.
/// Implements hybrid Web2/Web3 identity pattern per AGENTS.md architectural requirements.
/// </summary>
public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    EscrowDbContext dbContext)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
            return RegisterResult.FailureResult("Passwords do not match.");

        if (!AppRoles.All.Contains(request.Role))
            return RegisterResult.FailureResult($"Invalid role. Allowed values: {string.Join(", ", AppRoles.All)}.");

        // Create Actor first (domain entity) — hybrid identity bridge pattern
        var actor = new Actor
        {
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
            // WalletAddress remains null until Web3 link (future)
        };

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 1: Create Actor in domain
            dbContext.Actors.Add(actor);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Step 2: Create ApplicationUser with ActorId FK bridge
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                ActorId = actor.Id  // Bridge: ApplicationUser → Actor
            };

            var createResult = await userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return RegisterResult.FailureResult(errors);
            }

            // Step 3: Assign role — role must exist (seeded on startup via AppRoles)
            var roleResult = await userManager.AddToRoleAsync(user, request.Role);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                return RegisterResult.FailureResult($"Role assignment failed: {errors}");
            }

            await transaction.CommitAsync(cancellationToken);
            return RegisterResult.SuccessResult();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RegisterResult.FailureResult($"Registration failed: {ex.Message}");
        }
    }
}
