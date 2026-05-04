namespace WoongBlog.Infrastructure.LoadTesting;

public static class RealLoadTestCatalog
{
    private static readonly HashSet<string> AllowedScenarios = new(StringComparer.OrdinalIgnoreCase)
    {
        "public-api-rps",
        "public-api-spike",
        "public-api-soak",
        "public-api-stress"
    };

    private static readonly HashSet<string> AllowedRunners = new(StringComparer.OrdinalIgnoreCase)
    {
        "k6",
        "nbomber",
        "wrk",
        "autocannon",
        "fake"
    };

    private static readonly HashSet<string> AllowedTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "public-api-mix",
        "public-works-only",
        "public-blogs-only"
    };

    public static bool IsAllowedScenario(string scenario)
    {
        return AllowedScenarios.Contains(scenario);
    }

    public static bool IsAllowedRunner(string runner)
    {
        return AllowedRunners.Contains(runner);
    }

    public static bool IsAllowedTarget(string target)
    {
        return AllowedTargets.Contains(target);
    }

    public static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
