using Inventario.Products.Common;
using Inventario.Products.Dtos;

namespace Inventario.Products.Services;

public interface IProductsService
{
    Task<PagedResult<ProductResponse>> ListAsync(ProductQuery query, CancellationToken ct);
    Task<ServiceResult<ProductResponse>> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ProductResponse>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct);
    Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken ct);
    Task<ServiceResult<ProductResponse>> UpdateAsync(int id, ProductRequest request, CancellationToken ct);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct);
    Task<ServiceResult<ProductResponse>> AdjustStockAsync(int id, int delta, CancellationToken ct);
}
