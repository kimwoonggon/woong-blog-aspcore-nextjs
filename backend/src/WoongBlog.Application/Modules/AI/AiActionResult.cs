namespace WoongBlog.Application.Modules.AI;

public enum AiActionStatus
{
    Ok,
    BadRequest,
    NotFound,
    Conflict
}

public sealed record AiActionResult<T>(AiActionStatus Status, T? Value, string? Error = null)
{
    public static AiActionResult<T> Ok(T value) => new(AiActionStatus.Ok, value);
    public static AiActionResult<T> BadRequest(string error) => new(AiActionStatus.BadRequest, default, error);
    public static AiActionResult<T> NotFound() => new(AiActionStatus.NotFound, default);
    public static AiActionResult<T> Conflict(string error) => new(AiActionStatus.Conflict, default, error);
}
