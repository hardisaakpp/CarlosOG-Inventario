using System.ComponentModel.DataAnnotations;

namespace Inventario.Products.Dtos;

public class ProductQuery
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}