namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed class DatabaseDiagnosticsOptions
{
    public const string SectionName = "LoadTestDiagnostics:Database";

    public int LatencySampleCapacity { get; set; } = 512;

    public int SlowQuerySampleCapacity { get; set; } = 20;

    public double SlowQueryThresholdMs { get; set; } = 200;
}
