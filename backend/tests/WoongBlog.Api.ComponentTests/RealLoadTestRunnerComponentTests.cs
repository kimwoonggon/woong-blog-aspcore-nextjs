using WoongBlog.Infrastructure.LoadTesting;

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
}
