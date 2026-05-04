namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class LoadTestingOptions
{
    public const string SectionName = "LoadTesting";

    public bool UseFakeRunnerForTests { get; set; }

    public bool RealRunnerEnabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://127.0.0.1:3000";

    public string K6ExecutablePath { get; set; } = "k6";

    public string ReportsRelativeRoot { get; set; } = Path.Combine("reports", "loadtest");

    public int MinRate { get; set; } = 1;

    public int MaxRate { get; set; } = 5000;

    public int MinDurationSeconds { get; set; } = 1;

    public int MaxDurationSeconds { get; set; } = 3600;

    public int MinMaxVus { get; set; } = 1;

    public int MaxMaxVus { get; set; } = 10000;

    public int MaxConcurrentRuns { get; set; } = 1;
}
