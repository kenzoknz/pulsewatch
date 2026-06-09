namespace PulseWatch.Api.DTOs;

public class BulkCreateWebsitesDto
{
    public List<string> Urls { get; set; } = [];
    public int DefaultCheckIntervalSeconds { get; set; } = 300;
    public string NameStrategy { get; set; } = "auto";
}

public class BulkCreateWebsiteResultDto
{
    public List<WebsiteResponseDto> Created { get; set; } = [];
    public List<BulkWebsiteErrorDto> Skipped { get; set; } = [];
    public List<BulkWebsiteErrorDto> Failed { get; set; } = [];
    public BulkCreateWebsiteSummaryDto Summary { get; set; } = new();
}

public class BulkWebsiteErrorDto
{
    public string Url { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class BulkCreateWebsiteSummaryDto
{
    public int Total { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
}