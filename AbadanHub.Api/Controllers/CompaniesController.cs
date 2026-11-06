using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CompaniesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search = null, [FromQuery] string? sector = null)
    {
        var query = _db.CompanyPublications.AsNoTracking()
            .Where(x => !x.Company.IsArchived);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search) || x.Sector.Contains(search));
        if (!string.IsNullOrWhiteSpace(sector))
            query = query.Where(x => x.Sector == sector);
        var result = await query.OrderByDescending(x => x.IsTownshipVerified).ThenBy(x => x.Name)
            .Take(200).Select(x => new CompanySummary(x.CompanyId, x.Name, x.Sector,
                x.City, x.IsTownshipVerified, x.Description, x.Phone)).ToListAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var row = await _db.CompanyPublications.AsNoTracking()
            .Where(x => x.CompanyId == id && !x.Company.IsArchived)
            .Select(x => new CompanySummary(x.CompanyId, x.Name, x.Sector, x.City,
                x.IsTownshipVerified, x.Description, x.Phone)).FirstOrDefaultAsync();
        return row == null ? NotFound() : Ok(row);
    }

    [HttpGet("mine"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Mine()
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        return Ok(new CompanyMine(company.Id, new CompanyEdit
        {
            Name = company.Name, Sector = company.Sector, City = company.City,
            Description = company.Description, Phone = company.Phone,
            NationalIdentifier = company.NationalIdentifier, Address = company.Address,
            RequestsTownshipVerification = company.RequestsTownshipVerification
        }, company.Status, company.IsTownshipVerified, company.ReviewNote));
    }

    [HttpPost("save"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Save([FromBody] CompanyEdit input)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        company.Name = input.Name.Trim(); company.Sector = input.Sector.Trim();
        company.City = input.City.Trim(); company.Description = input.Description?.Trim();
        company.Phone = input.Phone?.Trim(); company.Address = input.Address?.Trim();
        company.NationalIdentifier = input.NationalIdentifier?.Trim();
        company.RequestsTownshipVerification = input.RequestsTownshipVerification;
        company.Status = ApprovalStatus.Pending;
        company.ReviewNote = null;
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("پیش‌نویس ذخیره و برای بررسی ارسال شد؛ نسخه منتشرشده تغییر نکرد."));
    }
}
