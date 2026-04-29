using EscrowApp.Models;

namespace EscrowApp.Models.Repositories;

public interface IEscrowTransactionRepository
{
    Task<EscrowTransaction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EscrowTransaction?> GetByIdReadOnlyAsync(int id, CancellationToken ct = default);
    Task<EscrowTransaction?> GetByExternalReferenceAsync(string externalReference, CancellationToken ct = default);
    Task<EscrowTransaction> AddAsync(EscrowTransaction transaction, CancellationToken ct = default);
    Task UpdateAsync(EscrowTransaction transaction, CancellationToken ct = default);
    Task<(IReadOnlyList<EscrowTransaction> Items, int TotalCount)> ListAsync(
        string? statusFilter, int page, int pageSize, CancellationToken ct = default);
}
