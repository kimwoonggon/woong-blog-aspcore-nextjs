namespace WoongBlog.Infrastructure.LoadTesting;

public interface IRealLoadTestRunner
{
    Task RunAsync(RealLoadTestRunEntry run, CancellationToken cancellationToken);
}
