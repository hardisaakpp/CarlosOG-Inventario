namespace Inventario.Products.Dtos;

public record ProductResponse(
    int Id,
    string Name,
    string? Description,
    string Category,
    string? ImageUrl,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);