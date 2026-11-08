using System.Net.Http.Json;
using System.Text.Json;
using Inventario.Transactions.Dtos;
using Inventario.Transactions.Common;

namespace Inventario.Transactions.Clients;

/// <summary>Datos de un producto tal como los expone el microservicio de productos.</summary>
public record ProductInfo(int Id, string Name, decimal Price, int Stock);

public interface IProductClient
{
    Task<ServiceResult<ProductInfo>> GetAsync(int id, CancellationToken ct);
    Task<ServiceResult<IReadOnlyDictionary<int, ProductInfo>>> GetManyAsync(IEnumerable<int> ids, CancellationToken ct);
    Task<ServiceResult<ProductInfo>> AdjustStockAsync(int id, int delta, CancellationToken ct);
}

/// <summary>Cliente REST síncrono hacia ProductService.</summary>
public class ProductClient(HttpClient http, ILogger<ProductClient> logger) : IProductClient
{
    public Task<ServiceResult<ProductInfo>> GetAsync(int id, CancellationToken ct) =>
        SendAsync<ProductInfo>(() => http.GetAsync($"api/products/{id}", ct), ct);

    public async Task<ServiceResult<IReadOnlyDictionary<int, ProductInfo>>> GetManyAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0) return new(new Dictionary<int, ProductInfo>());

        var query = string.Join("&", distinct.Select(i => $"ids={i}"));
        var result = await SendAsync<List<ProductInfo>>(() => http.GetAsync($"api/products/by-ids?{query}", ct), ct);
        return result.Ok
            ? new(result.Value!.ToDictionary(p => p.Id))
            : ServiceResult<IReadOnlyDictionary<int, ProductInfo>>.FailFrom(result);
    }

    public Task<ServiceResult<ProductInfo>> AdjustStockAsync(int id, int delta, CancellationToken ct) =>
        SendAsync<ProductInfo>(() => http.PostAsJsonAsync($"api/products/{id}/stock-adjustments", new { delta }, ct), ct);

    /// <summary>Ejecuta la llamada y traduce la respuesta (o su fallo) a un ServiceResult.</summary>
    private async Task<ServiceResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
                return new((await response.Content.ReadFromJsonAsync<T>(ct))!);

            var status = (int)response.StatusCode;
            var detail = await ReadDetailAsync(response, ct);
            return status is 400 or 404 or 409
                ? ServiceResult<T>.Fail(status, detail ?? "El servicio de productos rechazó la solicitud.")
                : ServiceResult<T>.Fail(503, "El servicio de productos devolvió un error inesperado.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "No se pudo contactar con ProductService");
            return ServiceResult<T>.Fail(503, "El servicio de productos no está disponible. Inténtelo de nuevo más tarde.");
        }
    }

    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            return json.TryGetProperty("detail", out var d) ? d.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
