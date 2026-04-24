using Microsoft.Extensions.Options;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Infrastructure.Ai;

internal sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai",
        "azure",
        "codex"
    };

    private static readonly HashSet<string> SupportedReasoningEfforts = new(StringComparer.OrdinalIgnoreCase)
    {
        "low",
        "medium",
        "high",
        "xhigh"
    };

    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        var failures = new List<string>();

        if (!SupportedProviders.Contains(options.Provider))
        {
            failures.Add("Ai:Provider must be one of OpenAi, Azure, or Codex.");
        }

        if (options.CodexTimeoutMs <= 0)
        {
            failures.Add("Ai:CodexTimeoutMs must be greater than 0.");
        }

        if (options.BatchConcurrency <= 0)
        {
            failures.Add("Ai:BatchConcurrency must be greater than 0.");
        }

        if (options.BatchCompletedRetentionDays < 0)
        {
            failures.Add("Ai:BatchCompletedRetentionDays must be 0 or greater.");
        }

        if (string.Equals(options.Provider, "Codex", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(options.CodexCommand))
        {
            failures.Add("Ai:CodexCommand is required when Ai:Provider is Codex.");
        }

        if (!string.IsNullOrWhiteSpace(options.AzureOpenAiEndpoint)
            && !Uri.TryCreate(options.AzureOpenAiEndpoint, UriKind.Absolute, out _))
        {
            failures.Add("Ai:AzureOpenAiEndpoint must be an absolute URI when provided.");
        }

        if (options.CodexAllowedModels.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("Ai:CodexAllowedModels cannot contain blank entries.");
        }

        if (options.CodexAllowedReasoningEfforts.Length == 0)
        {
            failures.Add("Ai:CodexAllowedReasoningEfforts must contain at least one value.");
        }
        else if (options.CodexAllowedReasoningEfforts.Any(value => !SupportedReasoningEfforts.Contains(value)))
        {
            failures.Add("Ai:CodexAllowedReasoningEfforts contains an unsupported value.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
