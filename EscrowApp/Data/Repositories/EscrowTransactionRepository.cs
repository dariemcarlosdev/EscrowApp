using EscrowApp.Models;
using EscrowApp.Models.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EscrowApp.Data.Repositories;

public sealed class EscrowTransactionRepository(EscrowDbContext context) : IEscrowTransactionRepository
{
    public async Task<EscrowTransaction?> GetByIdAsync(int id)
        => await context.Transactions.FindAsync(id);

    public async Task<EscrowTransaction> AddAsync(EscrowTransaction transaction)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(EscrowTransaction transaction)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
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
