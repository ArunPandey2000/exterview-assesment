using ExterView.Api.Models;
using System.Threading.Channels;

namespace ExterView.Api.Services;

public interface IBackgroundQueue
{
    Task EnqueueAsync(TranscriptProcessingMessage message);
    Task<TranscriptProcessingMessage> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundQueue : IBackgroundQueue
{
    private readonly Channel<TranscriptProcessingMessage> _queue;
    private readonly ILogger<BackgroundQueue> _logger;

    public BackgroundQueue(ILogger<BackgroundQueue> logger)
    {
        _logger = logger;
        // Create an unbounded channel with single reader/writer options
        _queue = Channel.CreateUnbounded<TranscriptProcessingMessage>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
    }

    public async Task EnqueueAsync(TranscriptProcessingMessage message)
    {
        await _queue.Writer.WriteAsync(message);
        _logger.LogInformation("Enqueued transcript processing for meeting {MeetingId}", message.MeetingId);
    }

    public async Task<TranscriptProcessingMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        var message = await _queue.Reader.ReadAsync(cancellationToken);
        _logger.LogInformation("Dequeued transcript processing for meeting {MeetingId}", message.MeetingId);
        return message;
    }
}
