using WoongBlog.Infrastructure.Persistence.Diagnostics;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class RequestDatabaseDiagnosticsTests
{
    [Fact]
    public void CaptureCurrent_ReturnsZero_WhenNoRequestScopeIsActive()
    {
        var diagnostics = new RequestDatabaseDiagnostics();

        diagnostics.RecordCommand(TimeSpan.FromMilliseconds(12));
        var snapshot = diagnostics.CaptureCurrent();

        Assert.Equal(0, snapshot.CommandCount);
        Assert.Equal(0, snapshot.CommandElapsedMs);
    }

    [Fact]
    public void CaptureCurrent_TracksCommandCountAndElapsedInsideRequestScope()
    {
        var diagnostics = new RequestDatabaseDiagnostics();

        using (diagnostics.BeginRequest())
        {
            diagnostics.RecordCommand(TimeSpan.FromMilliseconds(1.2));
            diagnostics.RecordCommand(TimeSpan.FromMilliseconds(2.3));

            var snapshot = diagnostics.CaptureCurrent();

            Assert.Equal(2, snapshot.CommandCount);
            Assert.Equal(3.5, snapshot.CommandElapsedMs);
        }

        var afterScope = diagnostics.CaptureCurrent();
        Assert.Equal(0, afterScope.CommandCount);
        Assert.Equal(0, afterScope.CommandElapsedMs);
    }
}
