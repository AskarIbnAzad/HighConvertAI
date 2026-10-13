namespace HighConvertAI.Server.Models.Scanning;

public class PageScanResult
{
    public PageScanResult()
    {
        
    }

    public PageScanResult(PageScanResult pageScanResult)
    {
        Url = pageScanResult.Url;
        PageType = pageScanResult.PageType;
    }

    public PageScanResult(string url, string pageType)
    {
        Url = url;
        PageType = pageType;
    }

    public string Url { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty;
}
