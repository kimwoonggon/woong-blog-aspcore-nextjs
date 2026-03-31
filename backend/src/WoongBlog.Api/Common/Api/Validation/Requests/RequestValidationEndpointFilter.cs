using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Common.Api.Validation.Requests;

internal sealed class RequestValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.FirstOrDefault(argument => argument?.GetType() == typeof(TRequest)) as TRequest;
        var validator = context.HttpContext.RequestServices.GetService(typeof(IValidator<TRequest>)) as IValidator<TRequest>;

        if (validator is null || request is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        return Results.ValidationProblem(validationResult.ToDictionary());
    }
}
