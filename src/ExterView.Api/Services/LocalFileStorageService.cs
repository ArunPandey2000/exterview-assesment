namespace ExterView.Api.Services;

public interface ILocalFileStorageService
{
    Task<string> SaveTranscriptAsync(string meetingId, string content);
}

public class LocalFileStorageService : ILocalFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _basePath;

    public LocalFileStorageService(
        ILogger<LocalFileStorageService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _basePath = _configuration.GetValue<string>("FileStorage:BasePath") ?? "./transcripts";
    }

    public async Task<string> SaveTranscriptAsync(string meetingId, string content)
    {
        // Create directory if it doesn't exist
        var directory = Path.Combine(_basePath, meetingId);
        Directory.CreateDirectory(directory);

        // Generate filename with timestamp
        var fileName = $"transcript_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
        var filePath = Path.Combine(directory, fileName);

        // Write transcript to file
        await File.WriteAllTextAsync(filePath, content);

        _logger.LogInformation("Transcript saved: {FilePath}", filePath);

        return filePath;
    }
}
