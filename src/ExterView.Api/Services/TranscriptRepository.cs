using ExterView.Api.Data;
using ExterView.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExterView.Api.Services;

public interface ITranscriptRepository
{
    Task<TranscriptMetadata> CreateAsync(TranscriptMetadata metadata);
    Task<TranscriptMetadata?> GetByMeetingIdAsync(string meetingId);
    Task UpdateAsync(TranscriptMetadata metadata);
}

public class TranscriptRepository : ITranscriptRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<TranscriptRepository> _logger;

    public TranscriptRepository(AppDbContext context, ILogger<TranscriptRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TranscriptMetadata> CreateAsync(TranscriptMetadata metadata)
    {
        _context.Transcripts.Add(metadata);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created transcript metadata: MeetingId={MeetingId}, Status={Status}",
            metadata.MeetingId, metadata.Status);

        return metadata;
    }

    public async Task<TranscriptMetadata?> GetByMeetingIdAsync(string meetingId)
    {
        return await _context.Transcripts
            .FirstOrDefaultAsync(t => t.MeetingId == meetingId);
    }

    public async Task UpdateAsync(TranscriptMetadata metadata)
    {
        metadata.UpdatedAt = DateTime.UtcNow;
        _context.Transcripts.Update(metadata);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated transcript metadata: MeetingId={MeetingId}, Status={Status}",
            metadata.MeetingId, metadata.Status);
    }
}
