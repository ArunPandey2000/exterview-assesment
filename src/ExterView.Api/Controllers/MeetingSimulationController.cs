using ExterView.Api.Models;
using ExterView.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExterView.Api.Controllers;

[ApiController]
[Route("api/meetings")]
public class MeetingSimulationController : ControllerBase
{
    private readonly IBackgroundQueue _queue;
    private readonly ILogger<MeetingSimulationController> _logger;

    public MeetingSimulationController(
        IBackgroundQueue queue,
        ILogger<MeetingSimulationController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Simulates a Teams meeting end event and triggers transcript processing
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulateMeeting([FromBody] SimulateMeetingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MeetingId))
        {
            return BadRequest(new { error = "MeetingId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            return BadRequest(new { error = "TenantId is required" });
        }

        _logger.LogInformation(
            "Meeting simulation request received: MeetingId={MeetingId}, TenantId={TenantId}",
            request.MeetingId, request.TenantId);

        // Generate transcript ID and idempotency key
        var transcriptId = Guid.NewGuid().ToString();
        var idempotencyKey = IdempotencyService.GenerateIdempotencyKey(request.MeetingId, transcriptId);

        // Create processing message
        var message = new TranscriptProcessingMessage
        {
            MeetingId = request.MeetingId,
            TranscriptId = transcriptId,
            TenantId = request.TenantId,
            MeetingEndTime = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };

        // Enqueue for processing
        await _queue.EnqueueAsync(message);

        _logger.LogInformation(
            "Transcript processing enqueued for meeting {MeetingId}",
            request.MeetingId);

        return Accepted(new
        {
            message = "Transcript processing queued successfully",
            meetingId = request.MeetingId,
            transcriptId = transcriptId,
            idempotencyKey = idempotencyKey,
            status = "Processing"
        });
    }

    /// <summary>
    /// Get transcript processing status by meeting ID
    /// </summary>
    [HttpGet("{meetingId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        string meetingId,
        [FromServices] ITranscriptRepository repository)
    {
        var metadata = await repository.GetByMeetingIdAsync(meetingId);

        if (metadata == null)
        {
            return NotFound(new { error = "Transcript not found for meeting ID" });
        }

        return Ok(new
        {
            meetingId = metadata.MeetingId,
            transcriptId = metadata.TranscriptId,
            status = metadata.Status,
            filePath = metadata.FilePath,
            processedAt = metadata.ProcessedAt,
            retryCount = metadata.RetryCount
        });
    }
}
