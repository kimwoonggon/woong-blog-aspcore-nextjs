using MediatR;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Modules.AI.Application.RuntimeConfig;

public sealed class GetAiRuntimeConfigQueryHandler(
    IOptions<AiOptions> options,
    IAiRuntimeCapabilities runtimeCapabilities)
    : IRequestHandler<GetAiRuntimeConfigQuery, AiRuntimeConfigResponse>
{
    private readonly AiOptions _options = options.Value;

    public Task<AiRuntimeConfigResponse> Handle(GetAiRuntimeConfigQuery request, CancellationToken cancellationToken)
    {
        var availableProviders = runtimeCapabilities.GetAvailableProviders();
        var configuredProvider = AiRuntimePolicy.NormalizeProvider(_options.Provider);
        var provider = availableProviders.Contains(configuredProvider, StringComparer.OrdinalIgnoreCase)
            ? configuredProvider
            : availableProviders[0];

        return Task.FromResult(new AiRuntimeConfigResponse(
            Provider: provider,
            AvailableProviders: availableProviders,
            DefaultModel: provider switch
            {
                "azure" => _options.AzureOpenAiDeployment,
                "codex" => _options.CodexModel,
                _ => _options.OpenAiModel,
            },
            CodexModel: _options.CodexModel,
            CodexReasoningEffort: _options.CodexReasoningEffort,
            AllowedCodexModels: _options.CodexAllowedModels,
            AllowedCodexReasoningEfforts: _options.CodexAllowedReasoningEfforts,
            BatchConcurrency: _options.BatchConcurrency,
            BatchCompletedRetentionDays: _options.BatchCompletedRetentionDays,
            DefaultSystemPrompt: runtimeCapabilities.GetDefaultBlogFixPrompt()));
    }
}
