using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Tests;

public class AdminAiDomainTests
{
    [Fact]
    public void CreateBlogFixJob_SetsExpectedInitialState()
    {
        var now = DateTimeOffset.UtcNow;

        var job = AiBatchJob.CreateBlogFixJob("selected", "2 selected", "key", false, true, 2, 2, "codex", "gpt-5.4", "medium", now);

        Assert.Equal(AiBatchJobStates.Queued, job.Status);
        Assert.Equal("selected", job.SelectionMode);
        Assert.True(job.AutoApply);
        Assert.Equal(now, job.CreatedAt);
        Assert.Equal(now, job.UpdatedAt);
    }

    [Fact]
    public void TerminalJob_CannotTransitionAgain()
    {
        var job = AiBatchJob.CreateBlogFixJob("selected", "1", "key", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        job.Finish(1, 1, 1, 0, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => job.Cancel(DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() => job.Start(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RecordSuccess_And_RecordFailure_UpdateItemState()
    {
        var item = AiBatchJobItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow);

        item.RecordSuccess("<p>fixed</p>", "fake", "model", "medium");
        Assert.Equal(AiBatchJobItemStates.Succeeded, item.Status);
        Assert.Equal("<p>fixed</p>", item.FixedHtml);

        item = AiBatchJobItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow);
        item.RecordFailure("boom");
        Assert.Equal(AiBatchJobItemStates.Failed, item.Status);
        Assert.Equal("boom", item.Error);
    }

    [Fact]
    public void ItemTerminalState_CannotBeMutatedAgain()
    {
        var item = AiBatchJobItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", DateTimeOffset.UtcNow);
        item.RecordSuccess("<p>fixed</p>", "fake", "model", null);
        item.MarkApplied(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => item.RecordFailure("boom"));
        Assert.Throws<InvalidOperationException>(() => item.Cancel(DateTimeOffset.UtcNow));
    }
}
