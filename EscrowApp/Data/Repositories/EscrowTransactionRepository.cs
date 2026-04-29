using EscrowApp.Models;
using EscrowApp.Models.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EscrowApp.Data.Repositories;

public sealed class EscrowTransactionRepository(EscrowDbContext context) : IEscrowTransactionRepository
{
    public async Task<EscrowTransaction?> GetByIdAsync(int id, CancellationToken ct = default)
        => await context.Transactions.FindAsync(new object[] { id }, ct);

    public async Task<EscrowTransaction?> GetByIdReadOnlyAsync(int id, CancellationToken ct = default)
        => await context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<EscrowTransaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default)
        => await context.Transactions.FirstOrDefaultAsync(t => t.ExternalReference == externalReference, ct);

    public async Task<EscrowTransaction> AddAsync(EscrowTransaction transaction, CancellationToken ct = default)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task UpdateAsync(EscrowTransaction transaction, CancellationToken ct = default)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<EscrowTransaction> Items, int TotalCount)> ListAsync(
        string? statusFilter, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<EscrowTransaction> query = context.Transactions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(t => t.Status == statusFilter);

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
