using System.Net;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Auth;

namespace WoongBlog.Api.Tests;

public sealed class BlogAiFixServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"blog-ai-fix-tests-{Guid.NewGuid():N}");

    public BlogAiFixServiceTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task FixHtmlAsync_CodexProvider_UsesWorkspaceWriteSandboxByDefault()
    {
        var capturePath = Path.Combine(_tempDirectory, "codex-invocation.txt");
        var commandPath = CreateCaptureCommand(capturePath);
        var service = CreateService(new AiOptions
        {
            Provider = "codex",
            CodexCommand = commandPath,
            CodexModel = "gpt-5.4",
            CodexReasoningEffort = "medium"
        });

        var result = await service.FixHtmlAsync("<p>Hello</p>", CancellationToken.None);

        Assert.Equal("<p>fixed</p>", result.FixedHtml);
        var captured = await File.ReadAllTextAsync(capturePath);
        Assert.Contains("ARGS=exec|-s|workspace-write|-c|model_reasoning_effort=\"medium\"|-m|gpt-5.4|--skip-git-repo-check|--ephemeral|-C|", captured, StringComparison.Ordinal);
        Assert.Contains("Return only the final HTML.", captured, StringComparison.Ordinal);
        Assert.Contains("<p>Hello</p>", captured, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FixHtmlAsync_CodexProvider_UsesConfiguredSandboxMode()
    {
        var capturePath = Path.Combine(_tempDirectory, "codex-danger-invocation.txt");
        var commandPath = CreateCaptureCommand(capturePath);
        var service = CreateService(new AiOptions
        {
            Provider = "codex",
            CodexCommand = commandPath,
            CodexSandboxMode = "danger-full-access"
        });

        await service.FixHtmlAsync("<p>Hello</p>", CancellationToken.None);

        var captured = await File.ReadAllTextAsync(capturePath);
        Assert.Contains("ARGS=exec|-s|danger-full-access|", captured, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // ignore temp cleanup failures
        }
    }

    private BlogAiFixService CreateService(AiOptions options)
    {
        var httpClient = new HttpClient(new StaticHttpMessageHandler());
        return new BlogAiFixService(
            httpClient,
            Options.Create(options),
            Options.Create(new AuthOptions()));
    }

    private string CreateCaptureCommand(string capturePath)
    {
        var commandPath = Path.Combine(_tempDirectory, $"capture-{Guid.NewGuid():N}.sh");
        File.WriteAllText(
            commandPath,
            $$"""
            #!/usr/bin/env bash
            set -euo pipefail
            stdin_contents="$(cat)"
            {
              printf 'ARGS='
              for arg in "$@"; do
                printf '%s|' "$arg"
              done
              printf '\nSTDIN=%s\n' "$stdin_contents"
            } > "{{capturePath}}"
            printf '<p>fixed</p>'
            """);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                commandPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        return commandPath;
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
    }
}
