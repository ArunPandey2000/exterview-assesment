using ExterView.Api.Data.Entities;
using ExterView.Api.Services;
using Polly;
using Polly.Retry;

namespace ExterView.Api.Workers;

public class TranscriptProcessorWorker : BackgroundService
{
    private readonly ILogger<TranscriptProcessorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TranscriptProcessorWorker(
        ILogger<TranscriptProcessorWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Create retry policy with exponential backoff
        var maxRetries = configuration.GetValue<int>("RetryPolicy:MaxRetries", 3);
        var initialDelay = configuration.GetValue<int>("RetryPolicy:InitialDelaySeconds", 1);

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * initialDelay),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} after {Delay}s delay. Error: {Error}",
                        retryCount, maxRetries, timeSpan.TotalSeconds, exception.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transcript Processor Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextMessageAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transcript message");
                // Continue processing next message
            }
        }

        _logger.LogInformation("Transcript Processor Worker stopped");
    }

    private async Task ProcessNextMessageAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue>();
        var mockGraphService = scope.ServiceProvider.GetRequiredService<IMockGraphService>();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<ILocalFileStorageService>();
        var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();
        var transcriptRepository = scope.ServiceProvider.GetRequiredService<ITranscriptRepository>();

        // Dequeue message (blocks until message available)
        var message = await queue.DequeueAsync(cancellationToken);

        _logger.LogInformation(
            "Processing transcript for meeting {MeetingId}, tenant {TenantId}",
            message.MeetingId, message.TenantId);

        // Check idempotency
        if (await idempotencyService.AlreadyProcessedAsync(message.IdempotencyKey, message.TenantId))
        {
            _logger.LogInformation(
                "Skipping already processed transcript: {MeetingId}",
                message.MeetingId);
            return;
        }

        // Create transcript metadata record
        var metadata = new TranscriptMetadata
        {
            MeetingId = message.MeetingId,
            TranscriptId = message.TranscriptId,
            TenantId = message.TenantId,
            Status = ProcessingStatus.Processing.ToString()
        };
        await transcriptRepository.CreateAsync(metadata);

        try
        {
            // Fetch transcript with retry logic
            var transcript = await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Fetching transcript for meeting {MeetingId}", message.MeetingId);
                return await mockGraphService.GetTranscriptAsync(message.MeetingId);
            });

            // Save transcript to file system
            var filePath = await fileStorageService.SaveTranscriptAsync(message.MeetingId, transcript);

            // Update metadata
            metadata.FilePath = filePath;
            metadata.Status = ProcessingStatus.Completed.ToString();
            metadata.ProcessedAt = DateTime.UtcNow;
            await transcriptRepository.UpdateAsync(metadata);

            // Mark as processed (idempotency)
            await idempotencyService.MarkAsProcessedAsync(
                message.IdempotencyKey,
                message.TenantId,
                "TranscriptProcessing");

            _logger.LogInformation(
                "Successfully processed transcript for meeting {MeetingId}, saved to {FilePath}",
                message.MeetingId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transcript for meeting {MeetingId}", message.MeetingId);

            // Update metadata to failed
            metadata.Status = ProcessingStatus.Failed.ToString();
            metadata.RetryCount++;
            await transcriptRepository.UpdateAsync(metadata);

            throw; // Re-throw to be caught by outer try-catch
        }
    }
}
