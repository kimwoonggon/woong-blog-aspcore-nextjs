using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Tests;

public sealed class CodexRuntimeEnvironmentTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"woong-codex-tests-{Guid.NewGuid():N}");

    public CodexRuntimeEnvironmentTests()
    {
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task FixHtmlAsync_CreatesCodexHomeDirectoryBeforeStartingCodex()
    {
        var codexHome = Path.Combine(_tempRoot, "codex-home");
        Directory.CreateDirectory(codexHome);
        await File.WriteAllTextAsync(Path.Combine(codexHome, "auth.json"), "{}");
        var scriptPath = CreateFakeCodexScript("""
#!/bin/sh
if [ ! -d "$CODEX_HOME" ]; then
  echo "missing codex home" >&2
  exit 7
fi
cat >/dev/null
printf '<p>fixed codex html</p>'
""");
        var service = CreateService(scriptPath, codexHome);

        var result = await service.FixHtmlAsync("<p>original</p>", CancellationToken.None, new AiFixRequestOptions(Provider: "codex"));

        Assert.True(Directory.Exists(codexHome));
        Assert.Equal("codex", result.Provider);
        Assert.Contains("fixed codex html", result.FixedHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FixHtmlAsync_FailsClearlyWhenCodexHomeIsAFile()
    {
        var codexHome = Path.Combine(_tempRoot, "codex-home");
        await File.WriteAllTextAsync(codexHome, "not a directory");
        var scriptPath = CreateFakeCodexScript("""
#!/bin/sh
printf '<p>should not run</p>'
""");
        var service = CreateService(scriptPath, codexHome);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FixHtmlAsync("<p>original</p>", CancellationToken.None, new AiFixRequestOptions(Provider: "codex")));

        Assert.Contains("Codex home must be a directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private BlogAiFixService CreateService(string commandPath, string codexHome)
    {
        var options = Options.Create(new AiOptions
        {
            Provider = "Codex",
            CodexCommand = commandPath,
            CodexArguments = ["exec"],
            CodexHome = codexHome,
            CodexWorkdir = _tempRoot,
            CodexModel = "gpt-5.4",
            CodexReasoningEffort = "medium",
            CodexTimeoutMs = 5000,
            CodexAllowedModels = ["gpt-5.4"],
            CodexAllowedReasoningEfforts = ["medium"]
        });
        var authOptions = Options.Create(new AuthOptions
        {
            MediaRoot = _tempRoot
        });

        return new BlogAiFixService(new HttpClient(), options, authOptions);
    }

    private string CreateFakeCodexScript(string content)
    {
        var scriptPath = Path.Combine(_tempRoot, $"fake-codex-{Guid.NewGuid():N}.sh");
        File.WriteAllText(scriptPath, content.Replace("\r\n", "\n"));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(scriptPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        return scriptPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
