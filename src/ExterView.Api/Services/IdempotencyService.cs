using ExterView.Api.Data;
using ExterView.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ExterView.Api.Services;

public interface IIdempotencyService
{
    Task<bool> AlreadyProcessedAsync(string idempotencyKey, string tenantId);
    Task MarkAsProcessedAsync(string idempotencyKey, string tenantId, string operationType);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(AppDbContext context, ILogger<IdempotencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> AlreadyProcessedAsync(string idempotencyKey, string tenantId)
    {
        var exists = await _context.IdempotencyRecords
            .AnyAsync(r => r.IdempotencyKey == idempotencyKey && r.TenantId == tenantId);

        if (exists)
        {
            _logger.LogInformation(
                "Idempotency check: Key {IdempotencyKey} already processed for tenant {TenantId}",
                idempotencyKey, tenantId);
        }

        return exists;
    }

    public async Task MarkAsProcessedAsync(string idempotencyKey, string tenantId, string operationType)
    {
        var record = new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            TenantId = tenantId,
            OperationType = operationType,
            StatusCode = 200,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.IdempotencyRecords.Add(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Marked operation as processed: Key={IdempotencyKey}, Tenant={TenantId}, Type={OperationType}",
            idempotencyKey, tenantId, operationType);
    }

    public static string GenerateIdempotencyKey(string meetingId, string transcriptId)
    {
        // Generate SHA256 hash as idempotency key
        var input = $"{meetingId}:{transcriptId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
