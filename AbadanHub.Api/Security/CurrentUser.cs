using System.Security.Claims;
using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Security;

public static class CurrentUser
{
    public static string? UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);
    public static bool IsManager(this ClaimsPrincipal user) =>
        user.IsInRole("TownManager") || user.IsInRole("SuperAdmin");
    public static async Task<Company?> MyCompanyAsync(this AppDbContext db, ClaimsPrincipal user) =>
        await db.Companies.FirstOrDefaultAsync(x => x.OwnerId == user.UserId() && !x.IsArchived);
    public static bool CanReadIssue(ClaimsPrincipal user, IssueReport issue)
    {
        if (user.IsManager() || issue.Company.OwnerId == user.UserId()) return true;
        return issue.Recipients.Any(x =>
            (x.Organization == AbadanHub.Shared.OrganizationKind.ChamberOfCommerce && user.IsInRole("ChamberOfCommerce")) ||
            (x.Organization == AbadanHub.Shared.OrganizationKind.AbadanRefinery && user.IsInRole("AbadanRefinery")) ||
            (x.Organization == AbadanHub.Shared.OrganizationKind.EngineersBasij && user.IsInRole("EngineersBasij")));
    }
}
