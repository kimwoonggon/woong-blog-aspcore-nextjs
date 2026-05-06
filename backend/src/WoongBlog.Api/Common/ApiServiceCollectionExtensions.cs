using System.IO.Compression;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.ResponseCompression;
using WoongBlog.Api.Common.Json;
using WoongBlog.Infrastructure.Validation;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace WoongBlog.Api.Common;

internal static class ApiServiceCollectionExtensions
{
    private static readonly string[] CompressedMimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(["application/json", "application/problem+json"])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static IServiceCollection AddApiCore(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationExceptionFilter>();
        });
        services.AddHealthChecks();
        services.AddOpenApi();
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = CompressedMimeTypes;
        });
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        services.Configure<HttpJsonOptions>(options => ConfigureHotPathJson(options.SerializerOptions));
        services.Configure<MvcJsonOptions>(options => ConfigureHotPathJson(options.JsonSerializerOptions));

        return services;
    }

    private static void ConfigureHotPathJson(JsonSerializerOptions options)
    {
        if (options.TypeInfoResolverChain.Contains(WoongBlogApiJsonSerializerContext.Default))
        {
            return;
        }

        options.TypeInfoResolverChain.Insert(0, WoongBlogApiJsonSerializerContext.Default);
    }
}
