using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Common.Api.Validation.Requests;

public static class EndpointBuilderExtensions
{
    public static RouteHandlerBuilder ValidateRequest<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder.AddEndpointFilter<RequestValidationEndpointFilter<TRequest>>();
    }
}
