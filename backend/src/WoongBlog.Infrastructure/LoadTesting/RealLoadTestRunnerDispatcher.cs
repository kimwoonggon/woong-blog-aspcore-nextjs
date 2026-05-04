namespace WoongBlog.Infrastructure.LoadTesting;

public sealed class RealLoadTestRunnerDispatcher(
    FakeRealLoadTestRunner fakeRunner,
    K6RealLoadTestRunner k6Runner)
    : IRealLoadTestRunner
{
    public Task RunAsync(RealLoadTestRunEntry run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (string.Equals(run.Runner, "fake", StringComparison.OrdinalIgnoreCase))
        {
            return fakeRunner.RunAsync(run, cancellationToken);
        }

        return k6Runner.RunAsync(run, cancellationToken);
    }
}
