using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WoongBlog.Api.Application.Content;

namespace WoongBlog.Api.Infrastructure.Validation;

public sealed class StoredJsonPresentationExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not StoredJsonPresentationException exception)
        {
            return Task.CompletedTask;
        }

        var details = new ProblemDetails
        {
            Title = "Stored content is invalid",
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError
        };
        details.Extensions["entityType"] = exception.EntityType;
        details.Extensions["entityKey"] = exception.EntityKey;
        details.Extensions["fieldName"] = exception.FieldName;

        context.Result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
