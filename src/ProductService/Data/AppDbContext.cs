using Inventario.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Products.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(150).IsRequired();
            e.Property(p => p.Description).HasMaxLength(500);
            e.Property(p => p.Category).HasMaxLength(100).IsRequired();
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.Property(p => p.Price).HasColumnType("numeric(18,2)");
            e.Property(p => p.CreatedAt).HasColumnType("timestamptz").HasDefaultValueSql("(NOW())");
            e.Property(p => p.UpdatedAt).HasColumnType("timestamptz");
            e.HasIndex(p => p.Name);
            e.HasIndex(p => p.Category);
        });
    }
}
