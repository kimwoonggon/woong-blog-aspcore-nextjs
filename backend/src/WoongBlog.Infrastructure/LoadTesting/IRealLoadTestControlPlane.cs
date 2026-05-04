namespace WoongBlog.Infrastructure.LoadTesting;

public interface IRealLoadTestControlPlane
{
    Task<RealLoadTestStartResponse> StartAsync(RealLoadTestStartRequest request, CancellationToken cancellationToken);

    Task<RealLoadTestStatusResponse> GetStatusAsync(string runId, CancellationToken cancellationToken);

    Task<RealLoadTestMetricsResponse> GetMetricsAsync(string runId, CancellationToken cancellationToken);

    Task<RealLoadTestStopResponse> StopAsync(string runId, CancellationToken cancellationToken);
}
