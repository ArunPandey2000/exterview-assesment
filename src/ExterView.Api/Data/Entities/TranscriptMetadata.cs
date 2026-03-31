using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExterView.Api.Data.Entities;

[Table("transcripts")]
public class TranscriptMetadata
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("meeting_id")]
    public string MeetingId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("transcript_id")]
    public string TranscriptId { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("file_path")]
    public string? FilePath { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = ProcessingStatus.Pending.ToString();

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
