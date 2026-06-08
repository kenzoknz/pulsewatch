using Microsoft.AspNetCore.Identity;

namespace PulseWatch.Api.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}