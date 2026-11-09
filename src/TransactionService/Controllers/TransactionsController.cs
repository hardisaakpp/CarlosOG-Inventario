using Inventario.Transactions.Common;
using Inventario.Transactions.Dtos;
using Inventario.Transactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.Transactions.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionsService transactions) : ControllerBase
{
    /// <summary>Historial de transacciones con filtros y paginación.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> List(
        [FromQuery] TransactionQuery query,
        CancellationToken ct) =>
        ToAction(await transactions.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Get(
        int id,
        CancellationToken ct) =>
        ToAction(await transactions.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(
        TransactionRequest request,
        CancellationToken ct)
    {
        var result = await transactions.CreateAsync(request, ct);

        if (!result.Ok)
            return Fail(result);

        var response = new ApiResponse<TransactionResponse>(
            result.Message ?? "Transacción creada correctamente.",
            result.Value!);

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Update(
        int id,
        TransactionRequest request,
        CancellationToken ct)
    {
        var result = await transactions.UpdateAsync(id, request, ct);

        if (!result.Ok)
            return Fail(result);

        return Ok(new ApiResponse<TransactionResponse>(
            result.Message ?? "Transacción actualizada correctamente.",
            result.Value!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await transactions.DeleteAsync(id, ct);

        if (!result.Ok)
            return Fail(result);

        return Ok(new ApiResponse<bool>(
            result.Message ?? "Transacción eliminada correctamente.",
            result.Value!));
    }

    private ActionResult<T> ToAction<T>(ServiceResult<T> result) =>
        result.Ok ? result.Value! : Fail(result);

    private ObjectResult Fail<T>(ServiceResult<T> result) =>
        Problem(detail: result.Error, statusCode: result.Status);
}