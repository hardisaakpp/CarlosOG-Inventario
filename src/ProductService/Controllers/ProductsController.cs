using Inventario.Products.Dtos;
using Inventario.Products.Services;
using Microsoft.AspNetCore.Mvc;
using Inventario.Products.Common;

namespace Inventario.Products.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductsService products) : ControllerBase
{
    /// <summary>Lista productos con filtros dinámicos y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> List([FromQuery] ProductQuery query, CancellationToken ct) =>
        await products.ListAsync(query, ct);

    /// <summary>Obtiene un producto por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken ct) =>
        ToAction(await products.GetAsync(id, ct));

    /// <summary>Obtiene varios productos en una sola llamada (lo usa el servicio de transacciones).</summary>
    [HttpGet("by-ids")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetByIds([FromQuery] int[] ids, CancellationToken ct) =>
        Ok(await products.GetByIdsAsync(ids.Distinct().ToArray(), ct));

    /// <summary>Lista las categorías existentes, ordenadas alfabéticamente.</summary>
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> Categories(CancellationToken ct) =>
        Ok(await products.GetCategoriesAsync(ct));

    /// <summary>Crea un producto.</summary>
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Actualiza un producto existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request, CancellationToken ct) =>
        ToAction(await products.UpdateAsync(id, request, ct));

    /// <summary>Elimina un producto.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await products.DeleteAsync(id, ct);
        return result.Ok ? NoContent() : Problem(detail: result.Error, statusCode: result.Status);
    }

    /// <summary>Ajuste atómico de stock. 409 si el resultado fuese negativo.</summary>
    [HttpPost("{id:int}/stock-adjustments")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> AdjustStock(int id, StockAdjustmentRequest request, CancellationToken ct) =>
        ToAction(await products.AdjustStockAsync(id, request.Delta, ct));

    private ActionResult<T> ToAction<T>(ServiceResult<T> result) =>
        result.Ok ? result.Value! : Problem(detail: result.Error, statusCode: result.Status);
}
