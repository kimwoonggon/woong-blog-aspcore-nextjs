using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.AI.Application.BatchJobs;

public interface IAiBatchJobScheduler
{
    Task ResetRunningJobsAsync(CancellationToken cancellationToken);
    Task ProcessQueuedJobsUntilEmptyAsync(CancellationToken cancellationToken);
}

public interface IAiBatchJobRunner
{
    Task RunAsync(Guid jobId, CancellationToken cancellationToken);
}

public interface IAiBatchJobItemDispatcher
{
    Task ProcessQueuedItemAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken);
    Task FinalizeJobAsync(Guid jobId, CancellationToken cancellationToken);
}

public interface IAiBatchJobItemProcessor
{
    Task ProcessAsync(Guid jobId, Guid itemId, CancellationToken cancellationToken);
}

public interface IAiBatchJobSignal
{
    void Notify();
}

public interface IBlogFixApplyPolicy
{
    void Apply(Blog blog, AiBatchJobItem item, string fixedHtml, DateTimeOffset timestamp);
}
