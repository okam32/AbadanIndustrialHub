using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    public JobsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        return Ok(await _db.JobVacancies.AsNoTracking().Where(x => x.IsOpen &&
            !x.Company.IsArchived && x.Company.Publication != null)
            .OrderByDescending(x => x.CreatedUtc).Take(200)
            .Select(x => new JobView(x.Id, x.Company.Publication!.Name, x.Title,
                x.Description, x.RequiredSkills, x.CreatedUtc)).ToListAsync());
    }

    [HttpPost("create"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Create([FromBody] JobEdit input)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        if (await _db.CompanyPublications.FindAsync(company.Id) == null)
            return Conflict(new ApiMessage("ابتدا اطلاعات شرکت باید تأیید شود."));
        _db.JobVacancies.Add(new JobVacancy { CompanyId = company.Id, Title = input.Title.Trim(),
            Description = input.Description.Trim(), RequiredSkills = input.RequiredSkills.Trim() });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("فرصت شغلی ثبت شد."));
    }

    [HttpPost("{id:guid}/apply"), Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Apply(Guid id)
    {
        var job = await _db.JobVacancies.FirstOrDefaultAsync(x => x.Id == id && x.IsOpen &&
            x.Company.Publication != null && !x.Company.IsArchived);
        if (job == null) return NotFound();
        var specialist = await _db.SpecialistProfiles.FirstOrDefaultAsync(x => x.UserId == User.UserId());
        if (specialist == null) return NotFound();
        if (await _db.JobApplications.AnyAsync(x => x.VacancyId == id && x.SpecialistProfileId == specialist.Id))
            return Conflict(new ApiMessage("قبلاً برای این فرصت درخواست ارسال کرده‌اید."));
        _db.JobApplications.Add(new JobApplication { VacancyId = id, SpecialistProfileId = specialist.Id });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("درخواست همکاری شما ثبت شد."));
    }

    [HttpGet("my-applications"), Authorize(Roles = "Specialist")]
    public async Task<IActionResult> MyApplications()
    {
        return Ok(await _db.JobApplications.AsNoTracking()
            .Where(x => x.SpecialistProfile.UserId == User.UserId())
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new { x.Vacancy.Title, x.Vacancy.Company.Name, x.CreatedUtc }).ToListAsync());
    }

    [HttpGet("applicants"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Applicants()
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        return Ok(await _db.JobApplications.AsNoTracking()
            .Where(x => x.Vacancy.CompanyId == company.Id)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new { Job = x.Vacancy.Title, Applicant = x.SpecialistProfile.FullName,
                x.SpecialistProfile.City, x.CreatedUtc }).ToListAsync());
    }
}
