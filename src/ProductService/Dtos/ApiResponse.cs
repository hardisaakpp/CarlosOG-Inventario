namespace Inventario.Products.Dtos;

public record ApiResponse<T>(string Message, T Data);