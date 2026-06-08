using Microsoft.AspNetCore.Identity;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = [AppRoles.Admin, AppRoles.User];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = "admin@pulsewatch.local";
        var adminUsername = "admin";
        if (await userManager.FindByEmailAsync(adminEmail) == null && await userManager.FindByNameAsync(adminUsername) == null)
        {
            var admin = new ApplicationUser
            {
                UserName       = adminUsername,
                Email          = adminEmail,
                EmailConfirmed = true,
                CreatedAt      = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, "Admin@123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}