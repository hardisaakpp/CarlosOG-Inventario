using Inventario.Transactions.Dtos;
using Inventario.Transactions.Common;

namespace Inventario.Transactions.Services;

public interface ITransactionsService
{
    Task<ServiceResult<PagedResult<TransactionResponse>>> ListAsync(TransactionQuery query, CancellationToken ct);
    Task<ServiceResult<TransactionResponse>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<TransactionResponse>> CreateAsync(TransactionRequest request, CancellationToken ct);
    Task<ServiceResult<TransactionResponse>> UpdateAsync(int id, TransactionRequest request, CancellationToken ct);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct);
}
