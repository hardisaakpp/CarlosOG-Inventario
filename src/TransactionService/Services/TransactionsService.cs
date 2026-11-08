using Inventario.Transactions.Clients;
using Inventario.Transactions.Data;
using Inventario.Transactions.Dtos;
using Inventario.Transactions.Models;
using Microsoft.EntityFrameworkCore;
using Inventario.Transactions.Common;

namespace Inventario.Transactions.Services;

/// <summary>
/// Orquesta el registro de transacciones: valida el stock y lo ajusta en ProductService
/// (llamada REST síncrona) y persiste la transacción. Como son dos escrituras en dos servicios
/// y no pueden ir en una sola transacción de base de datos, el orden es:
///   1) ajustar stock en ProductService;
///   2) guardar la transacción; si este paso falla, se revierte el ajuste del paso 1.
/// </summary>
public class TransactionsService(
    TransactionDbContext db,
    IProductClient products,
    ILogger<TransactionsService> logger) : ITransactionsService
{
    public async Task<ServiceResult<PagedResult<TransactionResponse>>> ListAsync(
        TransactionQuery q, CancellationToken ct)
    {
        IQueryable<InventoryTransaction> query = db.Transactions.AsNoTracking();

        if (q.ProductId.HasValue)
            query = query.Where(t => t.ProductId == q.ProductId);

        if (q.Type.HasValue)
            query = query.Where(t => t.Type == q.Type);

        if (q.From.HasValue)
        {
            var from = q.From.Value.UtcDateTime;
            query = query.Where(t => t.Date >= from);
        }

        if (q.To.HasValue)
        {
            var to = q.To.Value.UtcDateTime;
            query = query.Where(t => t.Date <= to);
        }

        var total = await query.CountAsync(ct);

        var page = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var infos = await products.GetManyAsync(
            page.Select(t => t.ProductId), ct);

        if (!infos.Ok)
            return ServiceResult<PagedResult<TransactionResponse>>.FailFrom(infos);

        var items = page.Select(t =>
            ToResponse(t, infos.Value!.GetValueOrDefault(t.ProductId))).ToList();

        return new(new PagedResult<TransactionResponse>(
            items, q.Page, q.PageSize, total));
    }

    public async Task<ServiceResult<TransactionResponse>> GetAsync(
        int id, CancellationToken ct)
    {
        var tx = await db.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tx is null)
            return NotFound(id);

        var infos = await products.GetManyAsync([tx.ProductId], ct);

        if (!infos.Ok)
            return ServiceResult<TransactionResponse>.FailFrom(infos);

        return new(ToResponse(
            tx, infos.Value!.GetValueOrDefault(tx.ProductId)));
    }

    public async Task<ServiceResult<TransactionResponse>> CreateAsync(
        TransactionRequest r, CancellationToken ct)
    {
        var product = await products.GetAsync(r.ProductId, ct);

        if (!product.Ok)
            return ServiceResult<TransactionResponse>.FailFrom(product);

        var tx = new InventoryTransaction();

        Map(r, tx, product.Value!);

        // Para una venta, ProductService rechaza (409) si el stock no alcanza.
        var adjusted = await products.AdjustStockAsync(
            tx.ProductId, tx.StockDelta, ct);

        if (!adjusted.Ok)
            return ServiceResult<TransactionResponse>.FailFrom(adjusted);

        try
        {
            db.Transactions.Add(tx);
            await db.SaveChangesAsync(ct);

            return ServiceResult<TransactionResponse>.Success(
                ToResponse(tx, adjusted.Value),
                "Transacción creada correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "No se pudo guardar la transacción {TransactionId} tras ajustar el stock del producto {ProductId}. Se ha revertido el ajuste.",
                tx.Id, tx.ProductId);

            await CompensateAsync([(tx.ProductId, tx.StockDelta)]);

            return ServiceResult<TransactionResponse>.Fail(
                500,
                "No se pudo guardar la transacción tras ajustar el stock del producto. Se ha revertido el ajuste.");
        }
    }

    public async Task<ServiceResult<TransactionResponse>> UpdateAsync(
        int id, TransactionRequest r, CancellationToken ct)
    {
        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tx is null)
            return NotFound(id);

        var newProduct = await products.GetAsync(r.ProductId, ct);

        if (!newProduct.Ok)
            return ServiceResult<TransactionResponse>.FailFrom(newProduct);

        var oldProductId = tx.ProductId;
        var oldDelta = tx.StockDelta;

        Map(r, tx, newProduct.Value!);

        var newDelta = tx.StockDelta;

        var applied = new List<(int ProductId, int Delta)>();
        var current = newProduct.Value!;

        if (oldProductId == tx.ProductId)
        {
            // Mismo producto: se aplica solo la diferencia neta.
            var net = newDelta - oldDelta;

            if (net != 0)
            {
                var adjusted = await products.AdjustStockAsync(
                    tx.ProductId, net, ct);

                if (!adjusted.Ok)
                    return ServiceResult<TransactionResponse>.FailFrom(adjusted);

                applied.Add((tx.ProductId, net));
                current = adjusted.Value!;
            }
        }
        else
        {
            // Cambio de producto: se aplica el nuevo efecto y se revierte el antiguo.
            var adjustedNew = await products.AdjustStockAsync(
                tx.ProductId, newDelta, ct);

            if (!adjustedNew.Ok)
                return ServiceResult<TransactionResponse>.FailFrom(adjustedNew);

            applied.Add((tx.ProductId, newDelta));
            current = adjustedNew.Value!;

            var revertedOld = await products.AdjustStockAsync(
                oldProductId, -oldDelta, ct);

            if (!revertedOld.Ok && revertedOld.Status != 404)
            {
                await CompensateAsync(applied);
                return ServiceResult<TransactionResponse>.FailFrom(revertedOld);
            }

            if (revertedOld.Ok)
                applied.Add((oldProductId, -oldDelta));
        }

        try
        {
            await db.SaveChangesAsync(ct);

            return ServiceResult<TransactionResponse>.Success(
                ToResponse(tx, current),
                "Transacción actualizada correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "No se pudo actualizar la transacción {TransactionId} tras ajustar el stock de los productos {ProductId} y {OldProductId}. Se han revertido los ajustes.",
                tx.Id, tx.ProductId, oldProductId);

            await CompensateAsync(applied);

            return ServiceResult<TransactionResponse>.Fail(
                500,
                "No se pudo actualizar la transacción tras ajustar el stock de los productos. Se han revertido los ajustes.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        int id, CancellationToken ct)
    {
        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tx is null)
            return ServiceResult<bool>.Fail(
                404, $"No existe la transacción con id {id}.");

        // Eliminar una transacción revierte su efecto sobre el stock.
        var reverted = await products.AdjustStockAsync(
            tx.ProductId, -tx.StockDelta, ct);

        if (!reverted.Ok && reverted.Status != 404)
        {
            return reverted.Status == 409
                ? ServiceResult<bool>.Fail(
                    409,
                    "No se puede eliminar la transacción: revertirla dejaría el stock del producto en negativo.")
                : ServiceResult<bool>.FailFrom(reverted);
        }

        try
        {
            db.Transactions.Remove(tx);
            await db.SaveChangesAsync(ct);

            return ServiceResult<bool>.Success(
                true, "Transacción eliminada correctamente.");
        }
        catch (Exception ex)
        {
            if (reverted.Ok)
                await CompensateAsync([(tx.ProductId, -tx.StockDelta)]);

            logger.LogError(
                ex,
                "No se pudo eliminar la transacción {TransactionId} tras revertir el stock del producto {ProductId}. Se ha intentado revertir el ajuste.",
                tx.Id, tx.ProductId);

            return ServiceResult<bool>.Fail(
                500,
                "No se pudo eliminar la transacción. El stock fue restaurado.");
        }
    }

    /// <summary>
    /// Deshace ajustes de stock ya aplicados cuando el resto de la operación falla.
    /// </summary>
    private async Task CompensateAsync(
        IEnumerable<(int ProductId, int Delta)> applied)
    {
        foreach (var (productId, delta) in applied.Reverse())
        {
            var result = await products.AdjustStockAsync(
                productId, -delta, CancellationToken.None);

            if (!result.Ok)
            {
                logger.LogError(
                    "No se pudo revertir el ajuste de stock ({Delta}) del producto {ProductId}: {Error}",
                    -delta, productId, result.Error);
            }
        }
    }

    private static ServiceResult<TransactionResponse> NotFound(int id) =>
        ServiceResult<TransactionResponse>.Fail(
            404, $"No existe la transacción con id {id}.");

    private static void Map(
        TransactionRequest r,
        InventoryTransaction tx,
        ProductInfo product)
    {
        tx.Date = (r.Date ?? DateTimeOffset.UtcNow).UtcDateTime;
        tx.Type = r.Type!.Value;
        tx.ProductId = r.ProductId;
        tx.Quantity = r.Quantity;

        // Redondear el precio unitario antes de calcular el total.
        tx.UnitPrice = Math.Round(
            r.UnitPrice ?? product.Price,
            2,
            MidpointRounding.AwayFromZero);

        tx.TotalPrice = Math.Round(
            tx.Quantity * tx.UnitPrice, 2);

        tx.Detail = string.IsNullOrWhiteSpace(r.Detail)
            ? null
            : r.Detail.Trim();
    }

    private static TransactionResponse ToResponse(
        InventoryTransaction t,
        ProductInfo? p) =>
        new(
            t.Id,
            t.Date,
            t.Type,
            t.ProductId,
            p?.Name ?? "(Producto eliminado)",
            p?.Stock ?? 0,
            t.Quantity,
            t.UnitPrice,
            t.TotalPrice,
            t.Detail);
}