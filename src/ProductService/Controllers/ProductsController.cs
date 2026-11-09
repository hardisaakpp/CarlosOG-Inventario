using Inventario.Products.Common;
using Inventario.Products.Dtos;
using Inventario.Products.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Products.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductsService products) : ControllerBase
{
    /// <summary>Lista productos con filtros dinámicos y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> List(
        [FromQuery] ProductQuery query,
        CancellationToken ct) =>
        await products.ListAsync(query, ct);

    /// <summary>Obtiene un producto por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken ct) =>
        ToAction(await products.GetAsync(id, ct));

    /// <summary>Obtiene varios productos por sus ids.</summary>
    [HttpGet("by-ids")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetByIds(
        [FromQuery] int[] ids,
        CancellationToken ct) =>
        Ok(await products.GetByIdsAsync(ids.Distinct().ToArray(), ct));

    /// <summary>Lista las categorías existentes.</summary>
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> Categories(CancellationToken ct) =>
        Ok(await products.GetCategoriesAsync(ct));

    /// <summary>Crea un producto.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ProductResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create(
        ProductRequest request,
        CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        var response = new ApiResponse<ProductResponse>(
            "Producto creado correctamente.",
            created);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, response);
    }

    /// <summary>Actualiza un producto existente. El stock no se modifica aquí: cambia solo con transacciones.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ApiResponse<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(
        int id,
        ProductRequest request,
        CancellationToken ct)
    {
        var result = await products.UpdateAsync(id, request, ct);

        if (!result.Ok)
            return Problem(detail: result.Error, statusCode: result.Status);

        return Ok(new ApiResponse<ProductResponse>(
            "Producto actualizado correctamente.",
            result.Value!));
    }

    /// <summary>Elimina un producto. Responde 409 si tiene transacciones registradas.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await products.DeleteAsync(id, ct);

        if (!result.Ok)
            return Problem(detail: result.Error, statusCode: result.Status);

        return Ok(new ApiResponse<bool>(
            "Producto eliminado correctamente.",
            result.Value!));
    }

    /// <summary>Ajuste atómico de stock.</summary>
    [HttpPost("{id:int}/stock-adjustments")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> AdjustStock(
        int id,
        StockAdjustmentRequest request,
        CancellationToken ct) =>
        ToAction(await products.AdjustStockAsync(id, request.Delta, ct));

    private ActionResult<T> ToAction<T>(ServiceResult<T> result) =>
        result.Ok ? result.Value! : Problem(detail: result.Error, statusCode: result.Status);
}