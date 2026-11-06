using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/specialists")]
public sealed class SpecialistsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SpecialistsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? skill, [FromQuery] string? city)
    {
        var query = _db.SpecialistProfiles.AsNoTracking().Where(x => x.IsPublic);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(x => x.City == city);
        if (!string.IsNullOrWhiteSpace(skill)) query = query.Where(x => x.Skills.Any(s => s.Name.Contains(skill)));
        return Ok(await query.OrderBy(x => x.FullName).Take(200)
            .Select(x => new SpecialistView(x.Id, x.FullName, x.City, x.Bio,
                x.Skills.Select(s => new SkillEdit { Name = s.Name, Level = s.Level,
                    YearsExperience = s.YearsExperience }).ToList())).ToListAsync());
    }

    [HttpGet("mine"), Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Mine()
    {
        var profile = await _db.SpecialistProfiles.AsNoTracking().Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == User.UserId());
        if (profile == null) return NotFound();
        return Ok(new SpecialistEdit { FullName = profile.FullName, City = profile.City,
            Bio = profile.Bio, IsPublic = profile.IsPublic,
            Skills = profile.Skills.Select(x => new SkillEdit { Name = x.Name, Level = x.Level,
                YearsExperience = x.YearsExperience }).ToList() });
    }

    [HttpPost("save"), Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Save([FromBody] SpecialistEdit input)
    {
        if (input.Skills.Count > 40) return BadRequest(new ApiMessage("حداکثر ۴۰ مهارت مجاز است."));
        if (input.Skills.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            return BadRequest(new ApiMessage("نام مهارت اجباری است."));
        var profile = await _db.SpecialistProfiles.Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == User.UserId());
        if (profile == null) return NotFound();
        profile.FullName = input.FullName.Trim(); profile.City = input.City.Trim();
        profile.Bio = input.Bio?.Trim(); profile.IsPublic = input.IsPublic;
        _db.SpecialistSkills.RemoveRange(profile.Skills);
        profile.Skills.Clear();
        foreach (var skill in input.Skills)
            profile.Skills.Add(new SpecialistSkill { Name = skill.Name.Trim(), Level = skill.Level,
                YearsExperience = skill.YearsExperience });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("رزومه با موفقیت ذخیره شد."));
    }
}
