using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;

namespace WoongBlog.Api.Infrastructure.Ai;

public sealed class BlogAiFixService : IBlogAiFixService
{
    private const int MaxCodexImages = 4;
    private static readonly Regex ImageRegex = new("""<img\b[^>]*?\bsrc=(?:""(?<src>[^""]+)""|'(?<src>[^']+)')[^>]*>""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const string PromptFileName = "ai-prompts.json";
    private static readonly AiPromptCatalog FallbackPrompts = new(
        BlogFix: """
You are an expert technical blog editor.
Your task is to clean up, enhance, and format the provided HTML content from a Tiptap editor.

Rules:
1. Rewrite the content into a cleaner, better-organized article while preserving the original meaning and technical depth.
2. Preserve every <img> tag. Keep image placement unless a nearby move clearly improves readability. Do not change the source URL.
3. Preserve code blocks, inline code, commands, filenames, and configuration snippets. If code is obviously malformed, you may repair it, but keep the original intent.
4. Improve headings, section structure, grammar, and flow. Add short explanatory paragraphs around images when the surrounding text is too thin.
5. Return valid HTML only. Do not emit markdown fences, commentary, or any wrapper outside the final HTML.
""",
        WorkEnrichTemplate: """
You are an expert technical portfolio editor and career coach.
Your task is to take a raw project description for "{title}" and transform it into a professional, compelling, and well-structured portfolio entry.

Rules:
1. Keep the output as valid HTML only.
2. Use <h2>, <p>, <ul>, and <li> for structure.
3. Expand vague phrasing into specific technical language.
4. Preserve all <img> tags and place them contextually.
5. Keep the output in the same language as the input.
""");

    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly AuthOptions _authOptions;
    private readonly AiPromptCatalog _prompts;

    public BlogAiFixService(HttpClient httpClient, IOptions<AiOptions> options, IOptions<AuthOptions> authOptions)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _authOptions = authOptions.Value;
        _prompts = LoadPromptCatalog();
    }

    public static string GetDefaultBlogFixPrompt() => LoadPromptCatalog().BlogFix;

    public async Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("HTML content is required.");
        }

        var requestOptions = options ?? new AiFixRequestOptions();
        var provider = NormalizeProvider(string.IsNullOrWhiteSpace(requestOptions.Provider) ? _options.Provider : requestOptions.Provider);

        return provider switch
        {
            "azure" => await FixWithAzureAsync(html, requestOptions, cancellationToken),
            "codex" => await FixWithCodexAsync(html, requestOptions, cancellationToken),
            _ => await FixWithOpenAiAsync(html, requestOptions, cancellationToken)
        };
    }

    private async Task<BlogAiFixResult> FixWithOpenAiAsync(string html, AiFixRequestOptions options, CancellationToken cancellationToken)
    {
        var apiKey = ResolveOpenAiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI is not configured.");
        }

        var model = ResolveOpenAiModel();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent(new
        {
            model,
            messages = BuildMessages(html, options)
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed: {payload}");
        }

        var fixedHtml = ExtractMessageContent(payload);
        return new BlogAiFixResult(CleanHtml(fixedHtml), "openai", model);
    }

    private async Task<BlogAiFixResult> FixWithAzureAsync(string html, AiFixRequestOptions options, CancellationToken cancellationToken)
    {
        var apiKey = ResolveAzureApiKey();
        var endpoint = ResolveAzureEndpoint();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI is not configured.");
        }

        endpoint = endpoint.TrimEnd('/');
        var deployment = ResolveAzureDeployment();
        var apiVersion = ResolveAzureApiVersion();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent(new
        {
            messages = BuildMessages(html, options)
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure OpenAI request failed: {payload}");
        }

        var fixedHtml = ExtractMessageContent(payload);
        return new BlogAiFixResult(CleanHtml(fixedHtml), "azure", deployment);
    }

    private async Task<BlogAiFixResult> FixWithCodexAsync(string html, AiFixRequestOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.CodexCommand))
        {
            throw new InvalidOperationException("Codex command is not configured.");
        }

        var codexHome = EnsureCodexHomeDirectory(_options.CodexHome);
        if (!HasCodexAuthentication(codexHome))
        {
            throw new InvalidOperationException("Codex is not authenticated on the server. Mount an authenticated CODEX_HOME directory or configure OPENAI_API_KEY.");
        }

        var model = ResolveCodexModel(options.CodexModel);
        var reasoningEffort = ResolveCodexReasoningEffort(options.CodexReasoningEffort);
        var prompt = BuildCodexPrompt(html, options);
        var imageArtifacts = await CollectImageArtifactsAsync(html, cancellationToken);
        var workdir = string.IsNullOrWhiteSpace(_options.CodexWorkdir) ? Directory.GetCurrentDirectory() : _options.CodexWorkdir;

        try
        {
            var arguments = new List<string>(_options.CodexArguments.Length > 0 ? _options.CodexArguments : ["exec"]);

            if (!arguments.Contains("--sandbox", StringComparer.OrdinalIgnoreCase))
            {
                arguments.Add("--sandbox");
                arguments.Add("workspace-write");
            }

            if (!arguments.Contains("--skip-git-repo-check", StringComparer.OrdinalIgnoreCase))
            {
                arguments.Add("--skip-git-repo-check");
            }

            if (!arguments.Contains("--ephemeral", StringComparer.OrdinalIgnoreCase))
            {
                arguments.Add("--ephemeral");
            }

            arguments.Add("-C");
            arguments.Add(workdir);
            arguments.Add("-");

            if (!string.IsNullOrWhiteSpace(model) && !arguments.Contains("-m"))
            {
                arguments.Insert(1, model);
                arguments.Insert(1, "-m");
            }

            if (!string.IsNullOrWhiteSpace(reasoningEffort) && !arguments.Contains("-c"))
            {
                arguments.Insert(1, $"model_reasoning_effort=\"{reasoningEffort}\"");
                arguments.Insert(1, "-c");
            }

            foreach (var artifact in imageArtifacts.AsEnumerable().Reverse())
            {
                arguments.Insert(1, artifact.Path);
                arguments.Insert(1, "-i");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.CodexCommand,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.Exists(workdir) ? workdir : Directory.GetCurrentDirectory()
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            startInfo.Environment["CODEX_HOME"] = codexHome;
            var codexHomeParent = Directory.GetParent(codexHome);
            if (codexHomeParent is not null && string.Equals(Path.GetFileName(codexHome), ".codex", StringComparison.Ordinal))
            {
                startInfo.Environment["HOME"] = codexHomeParent.FullName;
            }

            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Codex process.");
            }

            await process.StandardInput.WriteAsync(prompt);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(_options.CodexTimeoutMs, cancellationToken);

            var completed = await Task.WhenAny(waitTask, timeoutTask);
            if (completed == timeoutTask)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }

                throw new InvalidOperationException($"Codex request timed out after {_options.CodexTimeoutMs}ms.");
            }

            await waitTask;
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Codex request failed: {stderr.Trim()}");
            }

            return new BlogAiFixResult(CleanHtml(stdout), "codex", model, reasoningEffort);
        }
        finally
        {
            foreach (var artifact in imageArtifacts)
            {
                artifact.Dispose();
            }
        }
    }

    private static string EnsureCodexHomeDirectory(string configuredHome)
    {
        var codexHome = ResolveCodexHome(configuredHome);
        if (File.Exists(codexHome))
        {
            throw new InvalidOperationException(
                $"Codex home must be a directory, but '{codexHome}' is a file. Set CODEX_HOME or Ai:CodexHome to a writable directory.");
        }

        try
        {
            Directory.CreateDirectory(codexHome);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Codex home directory '{codexHome}' could not be created. Set CODEX_HOME or Ai:CodexHome to a writable directory.",
                exception);
        }

        return codexHome;
    }

    private static string ResolveCodexHome(string configuredHome)
    {
        if (!string.IsNullOrWhiteSpace(configuredHome))
        {
            return configuredHome.Trim();
        }

        var environmentHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(environmentHome))
        {
            return environmentHome.Trim();
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userHome)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".codex")
            : Path.Combine(userHome, ".codex");
    }

    private object[] BuildMessages(string html, AiFixRequestOptions options)
    {
        return
        [
            new { role = "system", content = BuildSystemPrompt(options) },
            new { role = "user", content = html }
        ];
    }

    private string BuildCodexPrompt(string html, AiFixRequestOptions options)
    {
        var builder = new StringBuilder();
        builder.AppendLine(BuildSystemPrompt(options));
        builder.AppendLine();
        builder.AppendLine("Return only the final HTML.");
        builder.AppendLine();
        builder.AppendLine(html);
        return builder.ToString();
    }

    private string BuildSystemPrompt(AiFixRequestOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CustomPrompt))
        {
            return options.CustomPrompt.Trim();
        }

        return options.Mode == AiFixMode.WorkEnrich
            ? _prompts.WorkEnrichTemplate.Replace("{title}", options.Title ?? "Untitled Project", StringComparison.Ordinal)
            : _prompts.BlogFix;
    }

    private async Task<IReadOnlyList<ImageArtifact>> CollectImageArtifactsAsync(string html, CancellationToken cancellationToken)
    {
        var artifacts = new List<ImageArtifact>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ImageRegex.Matches(html))
        {
            var src = WebUtility.HtmlDecode(match.Groups["src"].Value.Trim());
            if (string.IsNullOrWhiteSpace(src) || !seen.Add(src))
            {
                continue;
            }

            if (artifacts.Count >= MaxCodexImages)
            {
                break;
            }

            var artifact = await TryResolveImageAsync(src, cancellationToken);
            if (artifact is not null)
            {
                artifacts.Add(artifact);
            }
        }

        return artifacts;
    }

    private async Task<ImageArtifact?> TryResolveImageAsync(string src, CancellationToken cancellationToken)
    {
        if (src.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = src["/media/".Length..].Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_authOptions.MediaRoot, relative);
            if (File.Exists(physicalPath))
            {
                return new ImageArtifact(physicalPath, ownsFile: false);
            }

            return null;
        }

        if (!Uri.TryCreate(src, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = response.Content.Headers.ContentType?.MediaType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".bin"
            };
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"codex-image-{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
        return new ImageArtifact(tempPath, ownsFile: true);
    }

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }

    private static string ExtractMessageContent(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var choices = document.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var message = choices[0].GetProperty("message");
        if (message.TryGetProperty("content", out var content))
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string CleanHtml(string html)
    {
        return html.Replace("```html", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string NormalizeProvider(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "azureopenai" => "azure",
            "azure-openai" => "azure",
            "azure" => "azure",
            "codex" => "codex",
            _ => "openai"
        };
    }

    public static string[] GetAvailableProviders(AiOptions options)
    {
        var providers = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.OpenAiApiKey)
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            providers.Add("openai");
        }

        if ((!string.IsNullOrWhiteSpace(options.AzureOpenAiApiKey)
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")))
            && (!string.IsNullOrWhiteSpace(options.AzureOpenAiEndpoint)
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"))))
        {
            providers.Add("azure");
        }

        if (!string.IsNullOrWhiteSpace(options.CodexCommand)
            && HasCodexAuthentication(ResolveCodexHome(options.CodexHome)))
        {
            providers.Add("codex");
        }

        return providers.Count > 0 ? providers.ToArray() : ["openai"];
    }

    private string ResolveOpenAiApiKey() =>
        string.IsNullOrWhiteSpace(_options.OpenAiApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty
            : _options.OpenAiApiKey;

    private string ResolveOpenAiModel()
    {
        var value = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        return string.IsNullOrWhiteSpace(value) ? _options.OpenAiModel : value;
    }

    private string ResolveAzureApiKey() =>
        string.IsNullOrWhiteSpace(_options.AzureOpenAiApiKey)
            ? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? string.Empty
            : _options.AzureOpenAiApiKey;

    private string ResolveAzureEndpoint() =>
        string.IsNullOrWhiteSpace(_options.AzureOpenAiEndpoint)
            ? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty
            : _options.AzureOpenAiEndpoint;

    private string ResolveAzureDeployment()
    {
        var value = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_DEPLOYMENT_NAME");
        return string.IsNullOrWhiteSpace(value) ? _options.AzureOpenAiDeployment : value;
    }

    private string ResolveAzureApiVersion()
    {
        var value = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION");
        return string.IsNullOrWhiteSpace(value) ? _options.AzureOpenAiApiVersion : value;
    }

    private string ResolveCodexModel(string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? _options.CodexModel : overrideValue.Trim();
        return _options.CodexAllowedModels.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : _options.CodexModel;
    }

    private string ResolveCodexReasoningEffort(string? overrideValue)
    {
        var candidate = string.IsNullOrWhiteSpace(overrideValue) ? _options.CodexReasoningEffort : overrideValue.Trim().ToLowerInvariant();
        return _options.CodexAllowedReasoningEfforts.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            ? candidate
            : _options.CodexReasoningEffort;
    }

    private static bool HasCodexAuthentication(string codexHome)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(codexHome) || !Directory.Exists(codexHome))
        {
            return false;
        }

        return File.Exists(Path.Combine(codexHome, "auth.json"))
            || File.Exists(Path.Combine(codexHome, "credentials.json"));
    }

    private static AiPromptCatalog LoadPromptCatalog()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, PromptFileName);
            if (!File.Exists(path))
            {
                return FallbackPrompts;
            }

            var json = File.ReadAllText(path);
            var prompts = JsonSerializer.Deserialize<AiPromptCatalog>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return prompts is { BlogFix.Length: > 0, WorkEnrichTemplate.Length: > 0 }
                ? prompts
                : FallbackPrompts;
        }
        catch
        {
            return FallbackPrompts;
        }
    }

    private sealed class ImageArtifact(string path, bool ownsFile) : IDisposable
    {
        public string Path { get; } = path;

        public void Dispose()
        {
            if (ownsFile && File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}

internal sealed record AiPromptCatalog(string BlogFix, string WorkEnrichTemplate);
