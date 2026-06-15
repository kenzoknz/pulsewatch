namespace PulseWatch.Api.DTOs;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<string> Roles { get; set; } = [];
    public int TotalWebsites { get; set; }
}

public class AdminSystemStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalWebsites { get; set; }
    public int ActiveWebsites { get; set; }
    public int TotalUptimeChecksToday { get; set; }
    public int TotalDowntimeEventsOpen { get; set; }
}

public class ToggleUserActiveDto
{
    public bool IsActive { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "User";
}

public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}

public class AdminWebsiteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int CheckIntervalSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool? IsOnline { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
