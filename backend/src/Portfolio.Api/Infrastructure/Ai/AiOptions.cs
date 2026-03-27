namespace Portfolio.Api.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "OpenAi";
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o";
    public string AzureOpenAiApiKey { get; set; } = string.Empty;
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;
    public string AzureOpenAiDeployment { get; set; } = "gpt-5.2-chat";
    public string AzureOpenAiApiVersion { get; set; } = "2024-08-01-preview";
    public string CodexCommand { get; set; } = "codex";
    public string CodexModel { get; set; } = "gpt-5.4";
    public string CodexReasoningEffort { get; set; } = "medium";
    public int CodexTimeoutMs { get; set; } = 180000;
    public string CodexWorkdir { get; set; } = string.Empty;
    public int BatchConcurrency { get; set; } = 2;
    public int BatchCompletedRetentionDays { get; set; } = 3;
    public string[] CodexAllowedModels { get; set; } = [];
    public string[] CodexAllowedReasoningEfforts { get; set; } = [];
}
