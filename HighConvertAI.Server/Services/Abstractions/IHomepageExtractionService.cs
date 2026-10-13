using HighConvertAI.Server.Models.HomepageExtraction;

namespace HighConvertAI.Server.Services.Abstractions;

public interface IHomepageExtractionService
{
    public HomepageExtractionResult Extract(Models.Scanning.PageScanContext input);
}
