namespace ExterView.Api.Models;

public class TranscriptProcessingMessage
{
    public string MeetingId { get; set; } = string.Empty;
    public string TranscriptId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime MeetingEndTime { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
