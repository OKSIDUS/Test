using System.Threading.Channels;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.Infrastructure.Jobs;

public sealed class ImportJobQueue : IImportJobQueue
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryEnqueue() => _channel.Writer.TryWrite(true);

    public ValueTask WaitForJobAsync(CancellationToken ct) =>
        new(_channel.Reader.ReadAsync(ct).AsTask());
}
