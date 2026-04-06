using EscrowApp.Models;

namespace EscrowApp.Models.Repositories;

public interface IEscrowTransactionRepository
{
    Task<EscrowTransaction?> GetByIdAsync(int id);
    Task<EscrowTransaction> AddAsync(EscrowTransaction transaction);
    Task UpdateAsync(EscrowTransaction transaction);
    Task<(IReadOnlyList<EscrowTransaction> Items, int TotalCount)> ListAsync(
        string? statusFilter, int page, int pageSize, CancellationToken ct = default);
}
