using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/processes"), Authorize(Roles = "Company")]
public sealed class ProcessesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProcessesController(AppDbContext db) => _db = db;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        var list = await _db.WorkProcesses.AsNoTracking().Where(x => x.CompanyId == company.Id)
            .OrderBy(x => x.Title).Select(x => new ProcessView(x.Id, x.Title, x.Description,
                x.Steps.OrderBy(s => s.SortOrder).Select(s => s.Title).ToList())).ToListAsync();
        return Ok(list);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] ProcessEdit input)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        if (input.Steps.Count > 50 || input.Steps.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 160))
            return BadRequest(new ApiMessage("مراحل نامعتبر هستند (حداکثر ۵۰ مرحله)."));
        var entity = new WorkProcess { CompanyId = company.Id, Title = input.Title.Trim(),
            Description = input.Description?.Trim() };
        for (var i = 0; i < input.Steps.Count; i++)
            entity.Steps.Add(new ProcessStep { SortOrder = i, Title = input.Steps[i].Trim() });
        _db.WorkProcesses.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("فرایند ثبت شد."));
    }
}
