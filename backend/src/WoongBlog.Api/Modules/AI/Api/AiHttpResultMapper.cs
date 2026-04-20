using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Api;

internal static class AiHttpResultMapper
{
    public static IResult ToHttpResult<T>(this AiActionResult<T> result)
    {
        return result.Status switch
        {
            AiActionStatus.Ok => Results.Ok(result.Value),
            AiActionStatus.BadRequest => Results.BadRequest(new { error = result.Error }),
            AiActionStatus.NotFound => Results.NotFound(),
            AiActionStatus.Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
