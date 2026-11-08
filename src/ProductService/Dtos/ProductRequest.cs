using System.ComponentModel.DataAnnotations;

namespace Inventario.Products.Dtos;

public class ProductRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
}