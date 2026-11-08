namespace Inventario.Transactions.Common;

/// <summary>Resultado de una operación de negocio: un valor, o un error con su código HTTP.</summary>
public record ServiceResult<T>(T? Value, int Status = 200, string? Error = null, string? Message = null)
{
    public bool Ok => Error is null;

    public static ServiceResult<T> Success(T value, string message, int status = 200) =>
        new(value, status, null, message);

    public static ServiceResult<T> Fail(int status, string error) => new(default, status, error);

    /// <summary>Reutiliza el error de otro resultado (p. ej. el de ProductService).</summary>
    public static ServiceResult<T> FailFrom<TOther>(ServiceResult<TOther> other) => new(default, other.Status, other.Error);
}

