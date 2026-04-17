using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Ai;

internal sealed class AiOptionsPostConfigure(IConfiguration configuration) : IPostConfigureOptions<AiOptions>
{
    public void PostConfigure(string? name, AiOptions options)
    {
        options.Provider = FirstConfigured(configuration["AI_PROVIDER"], options.Provider, "OpenAi");
        options.OpenAiApiKey = FirstConfigured(configuration["OPENAI_API_KEY"], options.OpenAiApiKey);
        options.OpenAiModel = FirstConfigured(configuration["OPENAI_MODEL"], options.OpenAiModel, "gpt-4o");
        options.AzureOpenAiApiKey = FirstConfigured(configuration["AZURE_OPENAI_API_KEY"], options.AzureOpenAiApiKey);
        options.AzureOpenAiEndpoint = FirstConfigured(configuration["AZURE_OPENAI_ENDPOINT"], options.AzureOpenAiEndpoint);
        options.AzureOpenAiDeployment = FirstConfigured(
            configuration["AZURE_OPENAI_DEPLOYMENT"],
            configuration["AZURE_DEPLOYMENT_NAME"],
            options.AzureOpenAiDeployment,
            "gpt-5.2-chat");
        options.AzureOpenAiApiVersion = FirstConfigured(configuration["AZURE_OPENAI_API_VERSION"], options.AzureOpenAiApiVersion, "2024-08-01-preview");
        options.CodexCommand = FirstConfigured(configuration["CODEX_COMMAND"], options.CodexCommand, "codex");
        options.CodexArguments = ParseShellArguments(configuration["CODEX_ARGUMENTS"], options.CodexArguments, ["exec"]);
        options.CodexModel = FirstConfigured(configuration["CODEX_MODEL"], options.CodexModel, "gpt-5.4");
        options.CodexReasoningEffort = FirstConfigured(configuration["CODEX_REASONING_EFFORT"], options.CodexReasoningEffort, "medium");

        if (int.TryParse(configuration["CODEX_TIMEOUT_MS"], out var timeoutMs) && timeoutMs > 0)
        {
            options.CodexTimeoutMs = timeoutMs;
        }

        options.CodexWorkdir = FirstConfigured(configuration["CODEX_WORKDIR"], options.CodexWorkdir);
        options.CodexHome = FirstConfigured(configuration["CODEX_HOME"], options.CodexHome);

        if (int.TryParse(configuration["AI_BATCH_CONCURRENCY"], out var batchConcurrency) && batchConcurrency > 0)
        {
            options.BatchConcurrency = batchConcurrency;
        }

        if (int.TryParse(configuration["AI_BATCH_COMPLETED_RETENTION_DAYS"], out var retentionDays) && retentionDays >= 0)
        {
            options.BatchCompletedRetentionDays = retentionDays;
        }

        options.CodexAllowedModels = ParseCsv(
            configuration["CODEX_ALLOWED_MODELS"],
            options.CodexAllowedModels,
            ["gpt-5.4", "gpt-5.3-codex", "gpt-5.3-codex-spark"]);
        options.CodexAllowedReasoningEfforts = ParseCsv(
            configuration["CODEX_ALLOWED_REASONING_EFFORTS"],
            options.CodexAllowedReasoningEfforts,
            ["low", "medium", "high", "xhigh"]);
    }

    private static string FirstConfigured(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string[] ParseCsv(string? raw, IReadOnlyList<string> current, IReadOnlyList<string> fallback)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        if (current.Count > 0)
        {
            return current.ToArray();
        }

        return fallback.ToArray();
    }

    private static string[] ParseShellArguments(string? raw, IReadOnlyList<string> current, IReadOnlyList<string> fallback)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return System.Text.RegularExpressions.Regex.Matches(raw, "\"([^\"]*)\"|'([^']*)'|\\S+")
                .Select(match => match.Value.Trim().Trim('"', '\''))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        if (current.Count > 0)
        {
            return current.ToArray();
        }

        return fallback.ToArray();
    }
}
