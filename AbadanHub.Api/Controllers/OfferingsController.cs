using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/offerings")]
public sealed class OfferingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OfferingsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Published([FromQuery] Guid? companyId)
    {
        var query = _db.OfferingPublications.AsNoTracking().Where(x =>
            !x.Offering.IsArchived && !x.Offering.Company.IsArchived && x.Offering.Company.Publication != null);
        if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
        return Ok(await query.OrderBy(x => x.Kind).ThenBy(x => x.Title).Take(200)
            .Select(x => new OfferingView(x.OfferingId, x.CompanyId,
                x.Offering.Company.Publication!.Name, x.Title, x.Description, x.Kind)).ToListAsync());
    }

    [HttpGet("mine"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Mine()
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        return Ok(await _db.Offerings.AsNoTracking().Where(x => x.CompanyId == company.Id && !x.IsArchived)
            .Select(x => new { x.Id, x.Title, x.Description, x.Kind, x.Status, x.ReviewNote }).ToListAsync());
    }

    [HttpPost("save"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Save([FromBody] OfferingEdit input)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        Offering entity;
        if (input.Id.HasValue)
        {
            entity = await _db.Offerings.FirstOrDefaultAsync(x => x.Id == input.Id.Value &&
                x.CompanyId == company.Id && !x.IsArchived) ?? null!;
            if (entity == null) return NotFound();
        }
        else
        {
            entity = new Offering { CompanyId = company.Id };
            _db.Offerings.Add(entity);
        }
        entity.Title = input.Title.Trim(); entity.Description = input.Description.Trim();
        entity.Kind = input.Kind; entity.Status = ApprovalStatus.Pending; entity.ReviewNote = null;
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("خدمت/محصول ثبت شد و در انتظار تأیید است."));
    }

    [HttpPost("{id:guid}/archive"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        var offering = await _db.Offerings.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company.Id);
        if (offering == null) return NotFound();
        offering.IsArchived = true;
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("بایگانی شد."));
    }
}
