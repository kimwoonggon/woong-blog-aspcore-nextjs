using WoongBlog.Infrastructure.LoadTesting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class RealLoadTestRunnerComponentTests
{
    [Fact]
    public void K6Script_ExportsTrueP99ThresholdsAndNginxUpstreamAlias()
    {
        var script = K6RealLoadTestRunner.BuildScriptForTests();

        Assert.Contains("summaryTrendStats", script, StringComparison.Ordinal);
        Assert.Contains("'p(99)'", script, StringComparison.Ordinal);
        Assert.Contains("thresholds", script, StringComparison.Ordinal);
        Assert.Contains("http_req_failed", script, StringComparison.Ordinal);
        Assert.Contains("http_req_duration", script, StringComparison.Ordinal);
        Assert.Contains("X-Nginx-Upstream-Response-Time", script, StringComparison.Ordinal);
        Assert.Contains("nginx_upstream_waiting_fallback", script, StringComparison.Ordinal);
        Assert.Contains("response.timings.waiting", script, StringComparison.Ordinal);
    }

    [Fact]
    public void K6Script_RecordsTargetPayloadAndReceiveTimingForHeavyDetailAttribution()
    {
        var script = K6RealLoadTestRunner.BuildScriptForTests();

        Assert.Contains("response_bytes", script, StringComparison.Ordinal);
        Assert.Contains("response.timings.receiving", script, StringComparison.Ordinal);
        Assert.Contains("resolveResponseBodyBytes(response)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void K6Script_RecordsTargetDbCommandTimingForHeavyDetailAttribution()
    {
        var script = K6RealLoadTestRunner.BuildScriptForTests();

        Assert.Contains("target_${target.metricId}_db_command_elapsed", script, StringComparison.Ordinal);
        Assert.Contains("target_${target.metricId}_db_command_count", script, StringComparison.Ordinal);
        Assert.Contains("X-Db-Command-Elapsed-Ms", script, StringComparison.Ordinal);
        Assert.Contains("X-Db-Command-Count", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task K6Runner_PersistsRunScopedDiagnosticsWhileProcessIsRunning()
    {
        var tempRoot = CreateTempDirectory();
        try
        {
            var fakeK6Path = Path.Combine(tempRoot, "fake-k6");
            await File.WriteAllTextAsync(fakeK6Path, """
                #!/usr/bin/env bash
                set -euo pipefail
                summary_path=""
                while [ "$#" -gt 0 ]; do
                  if [ "$1" = "--summary-export" ]; then
                    shift
                    summary_path="$1"
                  fi
                  shift || true
                done
                sleep 1.3
                cat > "$summary_path" <<'JSON'
                {"metrics":{"http_reqs":{"count":1,"rate":1},"http_req_failed":{"passes":0},"http_req_duration":{"values":{"min":1,"med":1,"p(95)":1,"p(99)":1,"max":1}}}}
                JSON
                """, TestContext.Current.CancellationToken);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    fakeK6Path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var reportStore = new RealLoadTestReportStore(
                new TestHostEnvironment(tempRoot),
                Options.Create(new LoadTestingOptions
                {
                    BaseUrl = "http://nginx",
                    K6ExecutablePath = fakeK6Path,
                    ReportsRelativeRoot = "loadtest-reports"
                }));
            var diagnosticsSampler = new CountingDiagnosticsSampler();
            var runner = new K6RealLoadTestRunner(
                reportStore,
                diagnosticsSampler,
                Options.Create(new LoadTestingOptions
                {
                    BaseUrl = "http://nginx",
                    K6ExecutablePath = fakeK6Path
                }),
                NullLogger<K6RealLoadTestRunner>.Instance);
            var run = new RealLoadTestRunEntry(
                "component-k6-run-scoped-diagnostics",
                "k6",
                "public-api-rps",
                "public-api-mix",
                1,
                1,
                2,
                2,
                1,
                [new RealLoadTestTargetSpec("work-list", "Work list", "/api/public/works?page=1&pageSize=12", "work")],
                DateTimeOffset.UtcNow);

            await runner.RunAsync(run, TestContext.Current.CancellationToken);

            var metrics = await reportStore.ReadMetricsAsync(run.RunId, TestContext.Current.CancellationToken);
            var diagnosticsSamples = metrics.Where(metric => metric.Diagnostics is not null).ToArray();

            Assert.Equal(RealLoadTestRunStates.Completed, run.Status);
            Assert.True(diagnosticsSampler.CaptureCount >= 2);
            Assert.True(diagnosticsSamples.Length >= 2);
        }
        finally
        {
            DeleteDirectoryIfExists(tempRoot);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"woong-blog-k6-runner-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class CountingDiagnosticsSampler : ILoadTestDiagnosticsSampler
    {
        private int _captureCount;

        public int CaptureCount => Volatile.Read(ref _captureCount);

        public Task<LoadTestDiagnosticsSnapshot> CaptureAsync(CancellationToken cancellationToken)
        {
            var sample = Interlocked.Increment(ref _captureCount);
            return Task.FromResult(new LoadTestDiagnosticsSnapshot(
                DateTimeOffset.UtcNow,
                new LoadTestProcessDiagnostics(128 * 1024 * 1024 + sample, 2),
                new LoadTestGcDiagnostics(32 * 1024 * 1024 + sample, sample, 0, 0, 0),
                new LoadTestThreadPoolDiagnostics(4 + sample, sample, 100 + sample, 32760, 32767),
                new LoadTestDatabaseDiagnostics(
                    "available",
                    sample,
                    10 + sample,
                    sample,
                    10,
                    0,
                    new LoadTestDatabaseLatencyView(sample, sample, sample, sample),
                    new LoadTestDatabaseLatencyView(sample, 0, 0, 0),
                    0,
                    [],
                    0,
                    0,
                    null,
                    null)));
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "WoongBlog.Api.ComponentTests";

        public string ContentRootPath { get; set; } = contentRootPath;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
