using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WoongBlog.Infrastructure.Validation;

public sealed class ValidationExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ValidationException validationException)
        {
            return Task.CompletedTask;
        }

        var errors = validationException.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
