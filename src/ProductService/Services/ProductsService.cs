using Inventario.Products.Common;
using Inventario.Products.Data;
using Inventario.Products.Dtos;
using Inventario.Products.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventario.Products.Services;

public class ProductsService(AppDbContext db) : IProductsService
{
    public async Task<PagedResult<ProductResponse>> ListAsync(ProductQuery q, CancellationToken ct)
    {
        IQueryable<Product> query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Name))
        {
            // ILIKE: búsqueda sin distinguir mayúsculas (en PostgreSQL LIKE sí las distingue).
            var pattern = $"%{EscapeLike(q.Name.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern)
                                  || (p.Description != null && EF.Functions.ILike(p.Description, pattern)));
        }
        if (!string.IsNullOrWhiteSpace(q.Category)) query = query.Where(p => p.Category == q.Category);
        if (q.MinPrice.HasValue) query = query.Where(p => p.Price >= q.MinPrice);
        if (q.MaxPrice.HasValue) query = query.Where(p => p.Price <= q.MaxPrice);
        if (q.MinStock.HasValue) query = query.Where(p => p.Stock >= q.MinStock);
        if (q.MaxStock.HasValue) query = query.Where(p => p.Stock <= q.MaxStock);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);

        return new PagedResult<ProductResponse>(items, q.Page, q.PageSize, total);
    }

    public async Task<ServiceResult<ProductResponse>> GetAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? NotFound(id) : new(ToResponse(product));
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        ids.Count == 0
            ? []
            : await db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => ToResponse(p))
                .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct) =>
        await db.Products.AsNoTracking().Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync(ct);

    public async Task<ProductResponse> CreateAsync(ProductRequest r, CancellationToken ct)
    {
        var product = new Product { CreatedAt = DateTime.UtcNow };
        Map(r, product);
        // El stock inicial solo se fija al crear; después cambia únicamente con transacciones.
        product.Stock = r.Stock;
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return ToResponse(product);
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(int id, ProductRequest r, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound(id);

        Map(r, product);
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new(ToResponse(product));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
            return deleted == 0
                ? ServiceResult<bool>.Fail(404, $"No existe el producto con id {id}.")
                : new(true);
        }
        catch (Exception ex) when (IsForeignKeyViolation(ex))
        {
            // La clave foránea Transactions.ProductId impide borrar productos con movimientos.
            return ServiceResult<bool>.Fail(
                409, "No se puede eliminar el producto porque tiene transacciones registradas.");
        }
    }

    /// <summary>
    /// Ajusta el stock de forma atómica: la condición <c>Stock + delta &gt;= 0</c> se evalúa
    /// dentro del propio UPDATE, de modo que dos ventas concurrentes nunca dejan el stock negativo.
    /// </summary>
    public async Task<ServiceResult<ProductResponse>> AdjustStockAsync(int id, int delta, CancellationToken ct)
    {
        if (delta == 0)
            return ServiceResult<ProductResponse>.Fail(400, "El ajuste de stock debe ser distinto de cero.");
        var affected = await db.Products
            .Where(p => p.Id == id && p.Stock + delta >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock + delta)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

        if (affected == 0)
        {
            // 0 filas: o no existe el producto (404) o el stock no alcanza (409).
            var current = await db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => (int?)p.Stock)
                .FirstOrDefaultAsync(ct);

            return current is null
                ? NotFound(id)
                : ServiceResult<ProductResponse>.Fail(409, $"Stock insuficiente: disponible {current}, se requieren {-delta}.");
        }

        return await GetAsync(id, ct);
    }

    // ---------- helpers ----------

    private static ServiceResult<ProductResponse> NotFound(int id) =>
        ServiceResult<ProductResponse>.Fail(404, $"No existe el producto con id {id}.");

    private static bool IsForeignKeyViolation(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation }
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static void Map(ProductRequest r, Product p)
    {
        p.Name = r.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        p.Category = r.Category.Trim();
        p.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
        p.Price = r.Price;
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Category, p.ImageUrl, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);
}
