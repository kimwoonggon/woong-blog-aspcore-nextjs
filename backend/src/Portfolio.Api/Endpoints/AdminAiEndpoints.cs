using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Domain.Entities;
using Portfolio.Api.Infrastructure.Ai;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Endpoints;

public static class AdminAiEndpoints
{
    public static IEndpointRouteBuilder MapAdminAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ai")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI");

        group.MapGet("/runtime-config", RuntimeConfigAsync)
            .WithName("AdminAiRuntimeConfig")
            .WithSummary("Return the active AI runtime configuration used by admin tools.")
            .WithOpenApi();

        group.MapPost("/blog-fix", FixBlogAsync)
            .WithName("AdminAiFixBlog")
            .WithSummary("Fix a single blog HTML payload with the configured AI provider.")
            .WithOpenApi();

        group.MapPost("/blog-fix-batch", FixBlogBatchAsync)
            .WithName("AdminAiFixBlogBatch")
            .WithSummary("Fix one or more saved blogs, optionally applying the results.")
            .WithOpenApi();

        group.MapPost("/work-enrich", EnrichWorkAsync)
            .WithName("AdminAiEnrichWork")
            .WithSummary("Enrich a single work HTML payload with the configured AI provider.")
            .WithOpenApi();

        return app;
    }

    private static IResult RuntimeConfigAsync(IOptions<AiOptions> options)
    {
        var config = options.Value;
        return Results.Ok(new AiRuntimeConfigResponse(
            Provider: NormalizeProvider(config.Provider),
            DefaultModel: NormalizeProvider(config.Provider) switch
            {
                "azure" => config.AzureOpenAiDeployment,
                "codex" => config.CodexModel,
                _ => config.OpenAiModel,
            },
            CodexModel: config.CodexModel,
            CodexReasoningEffort: config.CodexReasoningEffort,
            AllowedCodexModels: config.CodexAllowedModels,
            AllowedCodexReasoningEfforts: config.CodexAllowedReasoningEfforts));
    }

    private static async Task<IResult> FixBlogAsync(
        BlogFixRequest request,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.BlogFix,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return Results.Ok(new BlogFixResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    private static async Task<IResult> FixBlogBatchAsync(
        BlogFixBatchRequest request,
        PortfolioDbContext dbContext,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Blogs.AsQueryable();
        if (!request.All)
        {
            if (request.BlogIds is null || request.BlogIds.Count == 0)
            {
                return Results.BadRequest(new { error = "Either blogIds or all=true is required." });
            }

            query = query.Where(blog => request.BlogIds.Contains(blog.Id));
        }

        var blogs = await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .ToListAsync(cancellationToken);

        var results = new List<BlogFixBatchItemResponse>(blogs.Count);
        foreach (var blog in blogs)
        {
            var html = AdminContentJson.ExtractHtml(blog.ContentJson);

            try
            {
                var aiResult = await aiFixService.FixHtmlAsync(html, cancellationToken, new AiFixRequestOptions(
                    Mode: AiFixMode.BlogFix,
                    CodexModel: request.CodexModel,
                    CodexReasoningEffort: request.CodexReasoningEffort));

                if (request.Apply)
                {
                    blog.ContentJson = $$"""{"html":{{JsonSerializer.Serialize(aiResult.FixedHtml)}}}""";
                    blog.Excerpt = AdminContentText.GenerateExcerpt(aiResult.FixedHtml);
                    blog.UpdatedAt = DateTimeOffset.UtcNow;
                }

                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "fixed",
                    aiResult.FixedHtml,
                    null,
                    aiResult.Provider,
                    aiResult.Model,
                    aiResult.ReasoningEffort));
            }
            catch (Exception exception)
            {
                results.Add(new BlogFixBatchItemResponse(
                    blog.Id,
                    blog.Title,
                    "failed",
                    null,
                    exception.Message,
                    null,
                    null,
                    null));
            }
        }

        if (request.Apply)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new BlogFixBatchResponse(results, request.Apply));
    }

    private static async Task<IResult> EnrichWorkAsync(
        WorkEnrichRequest request,
        IBlogAiFixService aiFixService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return Results.BadRequest(new { error = "HTML content is required." });
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.WorkEnrich,
            Title: request.Title,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort));
        return Results.Ok(new WorkEnrichResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }

    public sealed record BlogFixRequest(string Html, string? CodexModel = null, string? CodexReasoningEffort = null);

    public sealed record BlogFixResponse(string FixedHtml, string Provider, string Model, string? ReasoningEffort);

    public sealed record BlogFixBatchRequest(
        IReadOnlyList<Guid>? BlogIds,
        bool All,
        bool Apply,
        string? CodexModel = null,
        string? CodexReasoningEffort = null);

    public sealed record BlogFixBatchItemResponse(
        Guid BlogId,
        string Title,
        string Status,
        string? FixedHtml,
        string? Error,
        string? Provider,
        string? Model,
        string? ReasoningEffort);

    public sealed record BlogFixBatchResponse(
        IReadOnlyList<BlogFixBatchItemResponse> Results,
        bool Applied);

    public sealed record WorkEnrichRequest(
        string Html,
        string? Title = null,
        string? CodexModel = null,
        string? CodexReasoningEffort = null);

    public sealed record WorkEnrichResponse(
        string FixedHtml,
        string Provider,
        string Model,
        string? ReasoningEffort);

    public sealed record AiRuntimeConfigResponse(
        string Provider,
        string DefaultModel,
        string CodexModel,
        string CodexReasoningEffort,
        IReadOnlyList<string> AllowedCodexModels,
        IReadOnlyList<string> AllowedCodexReasoningEfforts);

    private static string NormalizeProvider(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };
}
