namespace PulseWatch.Api.DTOs;

// Bulk Check
public class BulkCheckRequestDto
{
    public List<int> WebsiteIds { get; set; } = [];
}

public class BulkCheckItemResultDto
{
    public int WebsiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int? StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BulkCheckSummaryDto
{
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}

// Bulk Delete
public class BulkDeleteRequestDto
{
    public List<int> WebsiteIds { get; set; } = [];
}

public class BulkDeleteResponseDto
{
    public int DeletedCount { get; set; }
}

// Check All
public class CheckAllResponseDto
{
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<BulkCheckItemResultDto> Results { get; set; } = [];
}

// Delete All
public class DeleteAllResponseDto
{
    public int DeletedCount { get; set; }
}