using System.ComponentModel.DataAnnotations;
using Inventario.Transactions.Models;

namespace Inventario.Transactions.Dtos;

public class TransactionRequest
{
    /// <summary>Instante de la transacción (ISO 8601 con zona, p. ej. "2026-11-01T14:30:00Z"). Si no se indica, se usa el actual.</summary>
    public DateTimeOffset? Date { get; set; }

    [Required(ErrorMessage = "El tipo de transacción es obligatorio.")]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "El tipo debe ser Purchase o Sale.")]
    public TransactionType? Type { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido.")]
    public int ProductId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe ser mayor que 0.")]
    public int Quantity { get; set; }

    /// <summary>Si no se indica se usa el precio actual del producto.</summary>
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "El precio unitario debe ser mayor que 0.")]
    public decimal? UnitPrice { get; set; }

    [StringLength(500, ErrorMessage = "El detalle no puede superar {1} caracteres.")]
    public string? Detail { get; set; }
}

public record TransactionResponse(
    int Id,
    DateTime Date,
    TransactionType Type,
    int ProductId,
    string ProductName,
    int ProductStock,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? Detail);

/// <summary>Filtros del historial. Todos son opcionales.</summary>
public class TransactionQuery
{
    public int? ProductId { get; set; }
    public TransactionType? Type { get; set; }

    /// <summary>Límites del rango como instantes (inclusive).</summary>
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}