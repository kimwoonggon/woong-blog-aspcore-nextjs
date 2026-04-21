using System.Threading.Channels;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;

namespace WoongBlog.Api.Infrastructure.Ai;

public sealed class AiBatchJobSignal : IAiBatchJobSignal
{
    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public void Notify()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _channel.Reader.ReadAsync(cancellationToken);
        while (_channel.Reader.TryRead(out _))
        {
        }
    }
}
