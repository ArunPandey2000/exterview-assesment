using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExterView.Api.Data.Entities;

[Table("idempotency_records")]
public class IdempotencyRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("idempotency_key")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("request_hash")]
    public string? RequestHash { get; set; }

    [Column("response_body")]
    public string? ResponseBody { get; set; }

    [Column("status_code")]
    public int? StatusCode { get; set; }

    [MaxLength(100)]
    [Column("operation_type")]
    public string? OperationType { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
