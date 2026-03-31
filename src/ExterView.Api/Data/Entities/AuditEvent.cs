using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExterView.Api.Data.Entities;

[Table("audit_events")]
public class AuditEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("actor_id")]
    public string? ActorId { get; set; }

    [MaxLength(50)]
    [Column("actor_type")]
    public string? ActorType { get; set; }

    [MaxLength(200)]
    [Column("resource_id")]
    public string? ResourceId { get; set; }

    [MaxLength(100)]
    [Column("resource_type")]
    public string? ResourceType { get; set; }

    [MaxLength(50)]
    [Column("action")]
    public string? Action { get; set; }

    [MaxLength(50)]
    [Column("result")]
    public string? Result { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    [Column("correlation_id")]
    public string? CorrelationId { get; set; }

    [MaxLength(50)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }
}
