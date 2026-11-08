using Inventario.Transactions.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Transactions.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<InventoryTransaction> Transactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InventoryTransaction>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(t => t.Id);
            e.Ignore(t => t.StockDelta);
            e.Property(t => t.Date).HasColumnType("timestamptz(0)");
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(10);
            e.Property(t => t.UnitPrice).HasColumnType("numeric(18,2)");
            e.Property(t => t.TotalPrice).HasColumnType("numeric(18,2)");
            e.Property(t => t.Detail).HasMaxLength(500);
            e.HasIndex(t => new { t.ProductId, t.Date });
        });
    }
}
