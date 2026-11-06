using AbadanHub.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AbadanHub.Api.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = new[] { "SuperAdmin", "TownManager", "Company", "Specialist",
            "ChamberOfCommerce", "AbadanRefinery", "EngineersBasij" };
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        var email = config["BootstrapAdmin:Email"];
        var password = config["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser { UserName = email, Email = email, EmailConfirmed = true,
                DisplayName = "مدیر سامانه" };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(
                "Bootstrap admin: " + string.Join(", ", result.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
            await userManager.AddToRoleAsync(user, "SuperAdmin");
    }
}
