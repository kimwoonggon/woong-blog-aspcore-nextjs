using System.Security.Cryptography;
using System.Text;

namespace WoongBlog.Application.Modules.AI;

internal static class AiRuntimePolicy
{
    public static string NormalizeProvider(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };

    public static string ResolveRequestedProvider(
        AiOptions options,
        IReadOnlyList<string> availableProviders,
        string? requestedProvider)
    {
        var normalizedRequested = NormalizeProvider(requestedProvider);
        if (!string.IsNullOrWhiteSpace(requestedProvider)
            && availableProviders.Contains(normalizedRequested, StringComparer.OrdinalIgnoreCase))
        {
            return normalizedRequested;
        }

        var runtimeProvider = NormalizeProvider(options.Provider);
        return availableProviders.Contains(runtimeProvider, StringComparer.OrdinalIgnoreCase)
            ? runtimeProvider
            : availableProviders[0];
    }

    public static string ResolveCodexModel(AiOptions options, string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? options.CodexModel : overrideValue.Trim();
        return options.CodexAllowedModels.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : options.CodexModel;
    }

    public static string ResolveCodexReasoningEffort(AiOptions options, string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? options.CodexReasoningEffort : overrideValue.Trim().ToLowerInvariant();
        return options.CodexAllowedReasoningEfforts.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : options.CodexReasoningEffort;
    }

    public static string BuildSelectionKey(
        string? selectionMode,
        IReadOnlyList<Guid> blogIds,
        string runtimeModel,
        string? runtimeReasoning,
        string? customPrompt,
        bool all,
        bool autoApply,
        int? workerCount)
    {
        var canonical = string.Join(
            '|',
            selectionMode ?? "selected",
            all ? "all" : "subset",
            runtimeModel,
            runtimeReasoning ?? string.Empty,
            customPrompt ?? string.Empty,
            autoApply ? "auto-apply" : "manual-apply",
            NormalizeWorkerCount(workerCount)?.ToString() ?? "default-workers",
            string.Join(",", blogIds.OrderBy(id => id)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static int? NormalizeWorkerCount(int? workerCount)
    {
        if (workerCount is null)
        {
            return null;
        }

        return Math.Clamp(workerCount.Value, 1, 8);
    }

    public static string? NormalizeCustomPrompt(string? customPrompt) =>
        string.IsNullOrWhiteSpace(customPrompt) ? null : customPrompt.Trim();
}
