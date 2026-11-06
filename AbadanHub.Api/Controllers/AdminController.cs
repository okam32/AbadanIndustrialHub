using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "SuperAdmin,TownManager")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("pending-companies")]
    public async Task<IActionResult> PendingCompanies() => Ok(await _db.Companies.AsNoTracking()
        .Where(x => x.Status == ApprovalStatus.Pending && !x.IsArchived)
        .OrderBy(x => x.CreatedUtc)
        .Select(x => new PendingCompany(x.Id, x.Name, x.Sector,
            x.RequestsTownshipVerification, x.CreatedUtc)).ToListAsync());

    [HttpGet("pending-offerings")]
    public async Task<IActionResult> PendingOfferings() => Ok(await _db.Offerings.AsNoTracking()
        .Where(x => x.Status == ApprovalStatus.Pending && !x.IsArchived)
        .OrderBy(x => x.Title)
        .Select(x => new PendingOffering(x.Id, x.Title, x.Company.Name)).ToListAsync());

    [HttpGet("companies/{id:guid}/review")]
    public async Task<IActionResult> ReviewCompany(Guid id)
    {
        var company = await _db.Companies.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Sector, x.City, x.Address, x.Phone,
                x.Description, x.NationalIdentifier, x.RequestsTownshipVerification,
                x.IsTownshipVerified, x.Status, x.ReviewNote }).FirstOrDefaultAsync();
        if (company == null) return NotFound();
        var documents = await _db.StoredFiles.AsNoTracking().Where(x => x.CompanyId == id)
            .Select(x => new { x.Id, x.OriginalName, x.Length }).ToListAsync();
        return Ok(new { company, documents });
    }

    [HttpGet("offerings/{id:guid}/review")]
    public async Task<IActionResult> ReviewOffering(Guid id)
    {
        var row = await _db.Offerings.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Title, x.Description, x.Kind, x.Status,
                CompanyName = x.Company.Name }).FirstOrDefaultAsync();
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost("companies/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCompany(Guid id, [FromBody] ApprovalDecision input)
    {
        var company = await _db.Companies.Include(x => x.Publication)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived);
        if (company == null) return NotFound();
        if (company.Status != ApprovalStatus.Pending) return Conflict(new ApiMessage("اطلاعات در انتظار تأیید نیست."));
        // Verification is only assigned by the manager, not by self-registration.
        company.IsTownshipVerified = company.IsTownshipVerified || (input.VerifyTownship && company.RequestsTownshipVerification);
        company.Status = ApprovalStatus.Approved;
        company.ReviewNote = null;
        var published = company.Publication ?? new CompanyPublication { CompanyId = company.Id };
        published.Name = company.Name; published.Sector = company.Sector; published.City = company.City;
        published.Description = company.Description; published.Phone = company.Phone;
        published.IsTownshipVerified = company.IsTownshipVerified;
        published.PublishedUtc = DateTime.UtcNow;
        if (company.Publication == null) _db.CompanyPublications.Add(published);
        _db.AuditEntries.Add(new AuditEntry { ActorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            Action = "CompanyApproved", TargetId = id.ToString() });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("شرکت تأیید و منتشر شد."));
    }

    [HttpPost("companies/{id:guid}/reject")]
    public async Task<IActionResult> RejectCompany(Guid id, [FromBody] ApprovalDecision input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) return BadRequest(new ApiMessage("دلیل رد الزامی است."));
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (company == null) return NotFound();
        if (company.Status != ApprovalStatus.Pending) return Conflict();
        company.Status = ApprovalStatus.Rejected;
        company.ReviewNote = input.Reason.Trim();
        _db.AuditEntries.Add(new AuditEntry { ActorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            Action = "CompanyRejected: " + input.Reason.Trim(), TargetId = id.ToString() });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("اطلاعات رد شد؛ نسخه منتشرشده محفوظ است."));
    }

    [HttpPost("offerings/{id:guid}/approve")]
    public async Task<IActionResult> ApproveOffering(Guid id)
    {
        var entity = await _db.Offerings.Include(x => x.Publication).FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived);
        if (entity == null) return NotFound();
        if (entity.Status != ApprovalStatus.Pending) return Conflict();
        if (!await _db.CompanyPublications.AnyAsync(x => x.CompanyId == entity.CompanyId))
            return Conflict(new ApiMessage("ابتدا شرکت باید تأیید شود."));
        var publication = entity.Publication ?? new OfferingPublication { OfferingId = entity.Id };
        publication.CompanyId = entity.CompanyId; publication.Title = entity.Title;
        publication.Description = entity.Description; publication.Kind = entity.Kind;
        publication.PublishedUtc = DateTime.UtcNow;
        if (entity.Publication == null) _db.OfferingPublications.Add(publication);
        entity.Status = ApprovalStatus.Approved;
        entity.ReviewNote = null;
        _db.AuditEntries.Add(new AuditEntry { ActorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            Action = "OfferingApproved", TargetId = id.ToString() });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("خدمت یا محصول منتشر شد."));
    }

    [HttpPost("offerings/{id:guid}/reject")]
    public async Task<IActionResult> RejectOffering(Guid id, [FromBody] ApprovalDecision input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) return BadRequest(new ApiMessage("دلیل رد الزامی است."));
        var entity = await _db.Offerings.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();
        if (entity.Status != ApprovalStatus.Pending) return Conflict();
        entity.Status = ApprovalStatus.Rejected;
        entity.ReviewNote = input.Reason.Trim();
        _db.AuditEntries.Add(new AuditEntry { ActorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            Action = "OfferingRejected: " + input.Reason.Trim(), TargetId = id.ToString() });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("خدمت یا محصول رد شد."));
    }
}
