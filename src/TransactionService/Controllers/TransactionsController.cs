using Inventario.Transactions.Dtos;
using Inventario.Transactions.Services;
using Microsoft.AspNetCore.Mvc;
using Inventario.Transactions.Common;

namespace Inventario.Transactions.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionsService transactions) : ControllerBase
{
    /// <summary>Historial de transacciones con filtros (producto, tipo, rango de fechas) y paginación.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> List([FromQuery] TransactionQuery query, CancellationToken ct) =>
        ToAction(await transactions.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Get(int id, CancellationToken ct) =>
        ToAction(await transactions.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(TransactionRequest request, CancellationToken ct)
    {
        var result = await transactions.CreateAsync(request, ct);
        return result.Ok ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : Fail(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionResponse>> Update(int id, TransactionRequest request, CancellationToken ct) =>
        ToAction(await transactions.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await transactions.DeleteAsync(id, ct);
        return result.Ok ? NoContent() : Fail(result);
    }

    private ActionResult<T> ToAction<T>(ServiceResult<T> result) => result.Ok ? result.Value! : Fail(result);

    private ObjectResult Fail<T>(ServiceResult<T> result) => Problem(detail: result.Error, statusCode: result.Status);
}
