using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Ai;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public class BlogAiFixServiceCodexRuntimeComponentTests
{
    [Fact]
    public void GetAvailableProviders_WithCodexCommand_IncludesOpenAiAndCodexChoices()
    {
        var providers = BlogAiFixService.GetAvailableProviders(new AiOptions
        {
            CodexCommand = "/bin/sh"
        });

        Assert.Contains("openai", providers);
        Assert.Contains("codex", providers);
    }

    [Fact]
    public async Task FixHtmlAsync_WithCodexProvider_FailsClearlyWhenCodexHomeIsFile()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var codexHomeFile = Path.Combine(tempRoot, "codex-home");
            await File.WriteAllTextAsync(codexHomeFile, "not a directory");
            var service = CreateService(new AiOptions
            {
                Provider = "Codex",
                CodexCommand = "codex-command-that-should-not-run",
                CodexHome = codexHomeFile
            });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.FixHtmlAsync("<p>Hello</p>", CancellationToken.None));

            Assert.Contains("Codex home must be a directory", exception.Message);
            Assert.Contains(codexHomeFile, exception.Message);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task FixHtmlAsync_WithCodexProvider_ExportsConfiguredOpenAiKeyToCodexProcess()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var codexHome = Path.Combine(tempRoot, "codex-home");
            Directory.CreateDirectory(codexHome);
            var scriptPath = Path.Combine(tempRoot, "fake-codex-openai-key.sh");
            await File.WriteAllTextAsync(scriptPath, """
                if [ "$OPENAI_API_KEY" != "configured-openai-key" ]; then
                  echo "missing configured openai key" >&2
                  exit 43
                fi

                cat >/dev/null
                printf '<p>codex used configured key</p>'
                """);

            var service = CreateService(new AiOptions
            {
                Provider = "Codex",
                OpenAiApiKey = "configured-openai-key",
                CodexCommand = "/bin/sh",
                CodexArguments = [scriptPath],
                CodexHome = codexHome,
                CodexWorkdir = tempRoot
            });

            var result = await service.FixHtmlAsync("<p>Hello</p>", CancellationToken.None);

            Assert.Equal("codex", result.Provider);
            Assert.Equal("<p>codex used configured key</p>", result.FixedHtml);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task FixHtmlAsync_WithCodexProvider_CreatesCodexHomeAndExportsItToProcess()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var codexHome = Path.Combine(tempRoot, "codex-home");
            Directory.CreateDirectory(codexHome);
            await File.WriteAllTextAsync(Path.Combine(codexHome, "auth.json"), "{}");
            var scriptPath = Path.Combine(tempRoot, "fake-codex.sh");
            await File.WriteAllTextAsync(scriptPath, """
                if [ ! -d "$CODEX_HOME" ]; then
                  echo "CODEX_HOME is not a directory: $CODEX_HOME" >&2
                  exit 42
                fi

                cat >/dev/null
                printf '<p>%s</p>' "$CODEX_HOME"
                """);

            var service = CreateService(new AiOptions
            {
                Provider = "Codex",
                CodexCommand = "/bin/sh",
                CodexArguments = [scriptPath],
                CodexHome = codexHome,
                CodexWorkdir = tempRoot
            });

            var result = await service.FixHtmlAsync("<p>Hello</p>", CancellationToken.None);

            Assert.True(Directory.Exists(codexHome));
            Assert.Equal($"<p>{codexHome}</p>", result.FixedHtml);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static BlogAiFixService CreateService(AiOptions options)
    {
        options.CodexAllowedModels = ["gpt-5.4"];
        options.CodexAllowedReasoningEfforts = ["medium"];
        options.CodexModel = "gpt-5.4";
        options.CodexReasoningEffort = "medium";
        options.CodexTimeoutMs = 5000;

        return new BlogAiFixService(
            new HttpClient(),
            Options.Create(options),
            Options.Create(new AuthOptions
            {
                MediaRoot = Path.GetTempPath()
            }));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"woong-blog-codex-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
