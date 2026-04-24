using FluentValidation;
using WoongBlog.Infrastructure.Validation;

namespace WoongBlog.Api.Common;

internal static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiCore(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationExceptionFilter>();
        });
        services.AddHealthChecks();
        services.AddOpenApi();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
