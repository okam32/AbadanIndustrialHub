using AbadanHub.Api.Data;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/reports"), Authorize(Roles =
    "SuperAdmin,TownManager,ChamberOfCommerce,AbadanRefinery,EngineersBasij")]
public sealed class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var companyQuery = _db.CompanyPublications.AsNoTracking().Where(x => !x.Company.IsArchived);
        var specialists = _db.SpecialistProfiles.AsNoTracking();
        var issueQuery = _db.IssueReports.AsNoTracking().AsQueryable();
        if (!User.IsManager())
        {
            var organizations = new List<OrganizationKind>();
            if (User.IsInRole("ChamberOfCommerce")) organizations.Add(OrganizationKind.ChamberOfCommerce);
            if (User.IsInRole("AbadanRefinery")) organizations.Add(OrganizationKind.AbadanRefinery);
            if (User.IsInRole("EngineersBasij")) organizations.Add(OrganizationKind.EngineersBasij);
            issueQuery = issueQuery.Where(x => x.Recipients.Any(r => organizations.Contains(r.Organization)));
        }
        var levelGroups = await _db.SpecialistSkills.GroupBy(x => x.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() }).ToListAsync();
        var issueGroups = await issueQuery.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        var report = new DashboardReport(
            await companyQuery.CountAsync(),
            await companyQuery.CountAsync(x => x.IsTownshipVerified),
            await specialists.CountAsync(),
            await _db.JobVacancies.CountAsync(x => x.IsOpen && x.Company.Publication != null),
            await issueQuery.CountAsync(x => x.Status != IssueStatus.Closed && x.Status != IssueStatus.Resolved),
            await companyQuery.GroupBy(x => x.Sector)
                .Select(g => new ReportSlice(g.Key, g.Count())).OrderByDescending(x => x.Count).ToListAsync(),
            await specialists.GroupBy(x => x.City)
                .Select(g => new ReportSlice(g.Key, g.Count())).OrderByDescending(x => x.Count).ToListAsync(),
            levelGroups.Select(x => new ReportSlice(x.Level.ToString(), x.Count)).ToList(),
            issueGroups.Select(x => new ReportSlice(x.Status.ToString(), x.Count)).ToList()
        );
        return Ok(report);
    }
}
