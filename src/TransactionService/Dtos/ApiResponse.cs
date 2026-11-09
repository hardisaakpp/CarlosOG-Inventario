namespace Inventario.Transactions.Dtos;

public record ApiResponse<T>(string Message, T Data);