namespace Labyrinth.Application;

/// <summary>Lightweight success/failure result so services avoid throwing for expected flows.</summary>
public readonly record struct ServiceResult<T>(bool Success, T? Value, string? Error)
{
    public static ServiceResult<T> Ok(T value) => new(true, value, null);
    public static ServiceResult<T> Fail(string error) => new(false, default, error);
}
