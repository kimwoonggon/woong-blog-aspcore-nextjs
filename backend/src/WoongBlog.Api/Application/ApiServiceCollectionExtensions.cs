using FluentValidation;
using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Behaviors;
using WoongBlog.Api.Infrastructure.Validation;

namespace WoongBlog.Api.Application;

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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IAdminUniqueSlugService, AdminUniqueSlugService>();
        services.AddScoped<IAdminExcerptService, AdminExcerptService>();

        return services;
    }
}
