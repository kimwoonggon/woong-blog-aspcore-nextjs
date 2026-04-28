using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Infrastructure.Ai;
using WoongBlog.Infrastructure.Auth;

namespace WoongBlog.Api.Tests;

[CollectionDefinition(AiProviderEnvironmentCollection.Name, DisableParallelization = true)]
public sealed class AiProviderEnvironmentCollection
{
    public const string Name = "AI provider environment variables";

    private AiProviderEnvironmentCollection()
    {
    }
}

[Collection(AiProviderEnvironmentCollection.Name)]
[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class BlogAiFixServiceProviderComponentTests
{
    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_ReturnsCleanedMessageContentAndSendsConfiguredModel()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>fixed html</p>"));
        var service = CreateService(handler, new AiOptions
        {
            Provider = "OpenAi",
            OpenAiApiKey = "configured-openai-key",
            OpenAiModel = "configured-openai-model"
        });

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal("openai", result.Provider);
        Assert.Equal("configured-openai-model", result.Model);
        Assert.Equal("<p>fixed html</p>", result.FixedHtml);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.openai.com/v1/chat/completions", request.RequestUri.OriginalString);
        Assert.Equal("Bearer", request.Authorization?.Scheme);
        Assert.Equal("configured-openai-key", request.Authorization?.Parameter);
        Assert.Equal("configured-openai-model", GetJsonString(request.Body, "model"));
    }

    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_CleansHtmlFenceFromResponse()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("""
            ```html
            <p>fenced html</p>
            ```
            """));
        var service = CreateOpenAiService(handler);

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal("<p>fenced html</p>", result.FixedHtml);
    }

    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_ThrowsWithPayloadWhenResponseFails()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.BadGateway, """{"error":"upstream failed"}"""));
        var service = CreateOpenAiService(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));

        Assert.Contains("OpenAI request failed", exception.Message);
        Assert.Contains("upstream failed", exception.Message);
    }

    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_ThrowsWhenResponseJsonIsMalformed()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.OK, "not-json"));
        var service = CreateOpenAiService(handler);

        await Assert.ThrowsAnyAsync<JsonException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));
    }

    [Theory]
    [InlineData("""{"choices":[]}""")]
    [InlineData("""{"choices":[{"message":{"role":"assistant"}}]}""")]
    public async Task FixHtmlAsync_WithOpenAiProvider_ReturnsEmptyHtmlWhenResponseHasNoMessageContent(string payload)
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.OK, payload));
        var service = CreateOpenAiService(handler);

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal(string.Empty, result.FixedHtml);
    }

    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_ThrowsWhenApiKeyIsMissing()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler();
        var service = CreateService(handler, new AiOptions
        {
            Provider = "OpenAi",
            OpenAiApiKey = string.Empty
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));

        Assert.Equal("OpenAI is not configured.", exception.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task FixHtmlAsync_WithUnknownProvider_FallsBackToOpenAi()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>openai fallback</p>"));
        var service = CreateService(handler, new AiOptions
        {
            Provider = "UnknownProvider",
            OpenAiApiKey = "configured-openai-key",
            OpenAiModel = "fallback-model"
        });

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal("openai", result.Provider);
        Assert.Equal("fallback-model", result.Model);
        Assert.Equal("<p>openai fallback</p>", result.FixedHtml);
    }

    [Fact]
    public async Task FixHtmlAsync_WithOpenAiProvider_UsesOpenAiModelEnvironmentOverride()
    {
        using var environment = EnvironmentVariableScope.Clear();
        environment.Set("OPENAI_MODEL", "environment-openai-model");
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>fixed html</p>"));
        var service = CreateService(handler, new AiOptions
        {
            Provider = "OpenAi",
            OpenAiApiKey = "configured-openai-key",
            OpenAiModel = "configured-openai-model"
        });

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("environment-openai-model", result.Model);
        Assert.Equal("environment-openai-model", GetJsonString(request.Body, "model"));
    }

    [Fact]
    public async Task FixHtmlAsync_WithAzureProvider_ReturnsCleanedMessageContent()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>azure fixed</p>"));
        var service = CreateAzureService(handler);

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal("azure", result.Provider);
        Assert.Equal("configured-deployment", result.Model);
        Assert.Equal("<p>azure fixed</p>", result.FixedHtml);
    }

    [Fact]
    public async Task FixHtmlAsync_WithAzureProvider_BuildsUrlWithoutDoubleSlashAndSendsApiKeyHeader()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>azure fixed</p>"));
        var service = CreateService(handler, new AiOptions
        {
            Provider = "Azure",
            AzureOpenAiApiKey = "configured-azure-key",
            AzureOpenAiEndpoint = "https://azure.example.com/",
            AzureOpenAiDeployment = "configured deployment",
            AzureOpenAiApiVersion = "2024-08-01-preview+beta"
        });

        await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        var requestUrl = request.RequestUri.OriginalString;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.StartsWith(
            "https://azure.example.com/openai/deployments/configured deployment/chat/completions",
            requestUrl,
            StringComparison.Ordinal);
        Assert.DoesNotContain("azure.example.com//openai", requestUrl);
        Assert.Contains("api-version=2024-08-01-preview%2Bbeta", requestUrl);
        Assert.True(request.Headers.TryGetValue("api-key", out var apiKeyValues));
        Assert.Equal(["configured-azure-key"], apiKeyValues);
    }

    [Fact]
    public async Task FixHtmlAsync_WithAzureProvider_ThrowsWithPayloadWhenResponseFails()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.InternalServerError, """{"error":"azure failed"}"""));
        var service = CreateAzureService(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));

        Assert.Contains("Azure OpenAI request failed", exception.Message);
        Assert.Contains("azure failed", exception.Message);
    }

    [Fact]
    public async Task FixHtmlAsync_WithAzureProvider_ThrowsWhenResponseJsonIsMalformed()
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.OK, "not-json"));
        var service = CreateAzureService(handler);

        await Assert.ThrowsAnyAsync<JsonException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));
    }

    [Theory]
    [InlineData("""{"choices":[]}""")]
    [InlineData("""{"choices":[{"message":{"role":"assistant"}}]}""")]
    public async Task FixHtmlAsync_WithAzureProvider_ReturnsEmptyHtmlWhenResponseHasNoMessageContent(string payload)
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler(JsonResponse(HttpStatusCode.OK, payload));
        var service = CreateAzureService(handler);

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        Assert.Equal(string.Empty, result.FixedHtml);
    }

    [Theory]
    [InlineData("", "https://azure.example.com")]
    [InlineData("configured-azure-key", "")]
    public async Task FixHtmlAsync_WithAzureProvider_ThrowsWhenApiKeyOrEndpointIsMissing(string apiKey, string endpoint)
    {
        using var environment = EnvironmentVariableScope.Clear();
        using var handler = new ScriptedHttpMessageHandler();
        var service = CreateService(handler, new AiOptions
        {
            Provider = "Azure",
            AzureOpenAiApiKey = apiKey,
            AzureOpenAiEndpoint = endpoint,
            AzureOpenAiDeployment = "configured-deployment"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FixHtmlAsync("<p>raw</p>", CancellationToken.None));

        Assert.Equal("Azure OpenAI is not configured.", exception.Message);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("AZURE_OPENAI_DEPLOYMENT", "environment-primary-deployment")]
    [InlineData("AZURE_DEPLOYMENT_NAME", "environment-legacy-deployment")]
    public async Task FixHtmlAsync_WithAzureProvider_UsesDeploymentEnvironmentOverride(string variableName, string deployment)
    {
        using var environment = EnvironmentVariableScope.Clear();
        environment.Set(variableName, deployment);
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>azure fixed</p>"));
        var service = CreateAzureService(handler);

        var result = await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(deployment, result.Model);
        Assert.Contains($"/deployments/{deployment}/", request.RequestUri.OriginalString);
    }

    [Fact]
    public async Task FixHtmlAsync_WithAzureProvider_UsesApiVersionEnvironmentOverride()
    {
        using var environment = EnvironmentVariableScope.Clear();
        environment.Set("AZURE_OPENAI_API_VERSION", "2025-01-01-preview+custom");
        using var handler = new ScriptedHttpMessageHandler(ChatCompletionResponse("<p>azure fixed</p>"));
        var service = CreateAzureService(handler);

        await service.FixHtmlAsync("<p>raw</p>", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Contains("api-version=2025-01-01-preview%2Bcustom", request.RequestUri.OriginalString);
    }

    private static BlogAiFixService CreateOpenAiService(ScriptedHttpMessageHandler handler)
    {
        return CreateService(handler, new AiOptions
        {
            Provider = "OpenAi",
            OpenAiApiKey = "configured-openai-key",
            OpenAiModel = "configured-openai-model"
        });
    }

    private static BlogAiFixService CreateAzureService(ScriptedHttpMessageHandler handler)
    {
        return CreateService(handler, new AiOptions
        {
            Provider = "Azure",
            AzureOpenAiApiKey = "configured-azure-key",
            AzureOpenAiEndpoint = "https://azure.example.com",
            AzureOpenAiDeployment = "configured-deployment",
            AzureOpenAiApiVersion = "2024-08-01-preview"
        });
    }

    private static BlogAiFixService CreateService(HttpMessageHandler handler, AiOptions options)
    {
        return new BlogAiFixService(
            new HttpClient(handler, disposeHandler: false),
            Options.Create(options),
            Options.Create(new AuthOptions
            {
                MediaRoot = Path.GetTempPath()
            }));
    }

    private static HttpResponseMessage ChatCompletionResponse(string content)
    {
        var payload = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content
                    }
                }
            }
        });

        return JsonResponse(HttpStatusCode.OK, payload);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    private static string GetJsonString(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(propertyName).GetString() ?? string.Empty;
    }

    private sealed class ScriptedHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedHttpRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedHttpRequest(
                request.Method,
                request.RequestUri ?? new Uri("about:blank"),
                request.Headers.Authorization,
                request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase),
                body));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted HTTP response was configured.");
            }

            return _responses.Dequeue();
        }
    }

    private sealed record RecordedHttpRequest(
        HttpMethod Method,
        Uri RequestUri,
        AuthenticationHeaderValue? Authorization,
        IReadOnlyDictionary<string, string[]> Headers,
        string Body);

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private static readonly string[] Names =
        [
            "OPENAI_API_KEY",
            "OPENAI_MODEL",
            "AZURE_OPENAI_API_KEY",
            "AZURE_OPENAI_ENDPOINT",
            "AZURE_OPENAI_DEPLOYMENT",
            "AZURE_DEPLOYMENT_NAME",
            "AZURE_OPENAI_API_VERSION"
        ];

        private readonly Dictionary<string, string?> _originalValues;

        private EnvironmentVariableScope()
        {
            _originalValues = Names.ToDictionary(
                name => name,
                Environment.GetEnvironmentVariable,
                StringComparer.Ordinal);
        }

        public static EnvironmentVariableScope Clear()
        {
            var scope = new EnvironmentVariableScope();
            foreach (var name in Names)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            return scope;
        }

        public void Set(string name, string value)
        {
            if (!_originalValues.ContainsKey(name))
            {
                throw new ArgumentException($"Environment variable '{name}' is not managed by this scope.", nameof(name));
            }

            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
