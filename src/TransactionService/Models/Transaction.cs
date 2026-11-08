namespace Inventario.Transactions.Models;

public enum TransactionType
{
    Purchase,
    Sale
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }

    /// <summary>Referencia lógica al producto del otro microservicio (sin FK entre servicios).</summary>
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Detail { get; set; }

    /// <summary>Efecto de la transacción sobre el stock: las compras suman, las ventas restan.</summary>
    public int StockDelta => Type == TransactionType.Purchase ? Quantity : -Quantity;
}
