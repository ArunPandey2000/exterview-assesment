using ExterView.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExterView.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TranscriptMetadata> Transcripts { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transcripts table configuration
        modelBuilder.Entity<TranscriptMetadata>(entity =>
        {
            entity.HasIndex(e => e.TenantId).HasDatabaseName("idx_transcripts_tenant_id");
            entity.HasIndex(e => e.MeetingId).HasDatabaseName("idx_transcripts_meeting_id");
            entity.HasIndex(e => new { e.MeetingId, e.TranscriptId })
                .IsUnique()
                .HasDatabaseName("uq_meeting_transcript");
        });

        // Idempotency records table configuration
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasIndex(e => new { e.IdempotencyKey, e.TenantId })
                .IsUnique()
                .HasDatabaseName("uq_idempotency_key");
        });

        // Audit events table configuration
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Timestamp })
                .HasDatabaseName("idx_audit_tenant_timestamp");
            entity.HasIndex(e => e.ResourceId).HasDatabaseName("idx_audit_resource_id");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("idx_audit_correlation_id");
        });
    }
}
