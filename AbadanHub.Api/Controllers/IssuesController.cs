using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/issues"), Authorize]
public sealed class IssuesController : ControllerBase
{
    private readonly AppDbContext _db;
    public IssuesController(AppDbContext db) => _db = db;

    private IQueryable<IssueReport> AuthorizedIssues()
    {
        var baseQuery = _db.IssueReports.AsNoTracking();
        if (User.IsManager()) return baseQuery;
        if (User.IsInRole("Company")) return baseQuery.Where(x => x.Company.OwnerId == User.UserId());
        var organizations = new List<OrganizationKind>();
        if (User.IsInRole("ChamberOfCommerce")) organizations.Add(OrganizationKind.ChamberOfCommerce);
        if (User.IsInRole("AbadanRefinery")) organizations.Add(OrganizationKind.AbadanRefinery);
        if (User.IsInRole("EngineersBasij")) organizations.Add(OrganizationKind.EngineersBasij);
        return baseQuery.Where(x => x.Recipients.Any(r => organizations.Contains(r.Organization)));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await AuthorizedIssues().OrderByDescending(x => x.CreatedUtc).Take(200)
            .Select(x => new IssueView(x.Id, x.Company.Name, x.Title, x.Description,
                x.Category, x.Priority, x.Status,
                x.Recipients.Select(r => r.Organization).ToList(), x.CreatedUtc)).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var row = await AuthorizedIssues().Where(x => x.Id == id)
            .Select(x => new IssueView(x.Id, x.Company.Name, x.Title, x.Description,
                x.Category, x.Priority, x.Status,
                x.Recipients.Select(r => r.Organization).ToList(), x.CreatedUtc)).FirstOrDefaultAsync();
        if (row == null) return NotFound();
        var history = await _db.IssueFollowUps.AsNoTracking().Where(x => x.IssueReportId == id)
            .OrderBy(x => x.CreatedUtc).Select(x => new IssueFollowUpView(x.Actor.DisplayName,
                x.Comment, x.Status, x.CreatedUtc)).ToListAsync();
        return Ok(new IssueDetail(row, history));
    }

    [HttpPost("create"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Create([FromBody] IssueEdit input)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        if (input.WorkProcessId.HasValue && !await _db.WorkProcesses.AnyAsync(x =>
            x.Id == input.WorkProcessId.Value && x.CompanyId == company.Id))
            return BadRequest(new ApiMessage("فرایند متعلق به شرکت شما نیست."));
        if (input.Recipients.Count > 3 || input.Recipients.Any(x => !Enum.IsDefined(x)))
            return BadRequest(new ApiMessage("نهادهای انتخاب‌شده نامعتبر هستند."));
        var issue = new IssueReport { CompanyId = company.Id, WorkProcessId = input.WorkProcessId,
            Title = input.Title.Trim(), Description = input.Description.Trim(), Category = input.Category.Trim(),
            Priority = input.Priority };
        foreach (var recipient in input.Recipients.Distinct())
            issue.Recipients.Add(new IssueRecipient { Organization = recipient });
        issue.FollowUps.Add(new IssueFollowUp { ActorUserId = User.UserId()!,
            Comment = "گزارش اولیه توسط شرکت ثبت شد.", Status = issue.Status });
        _db.IssueReports.Add(issue);
        await _db.SaveChangesAsync();
        return Ok(new { issue.Id, message = "مشکل صنعتی ثبت شد." });
    }

    private static bool AllowedTransition(IssueStatus current, IssueStatus next) =>
        current switch
        {
            IssueStatus.Submitted => next is IssueStatus.UnderReview or IssueStatus.Referred,
            IssueStatus.UnderReview => next is IssueStatus.Referred or IssueStatus.InProgress,
            IssueStatus.Referred => next == IssueStatus.InProgress,
            IssueStatus.InProgress => next == IssueStatus.Resolved,
            IssueStatus.Resolved => next is IssueStatus.Closed or IssueStatus.InProgress,
            IssueStatus.Closed => next == IssueStatus.UnderReview,
            _ => false
        };

    [HttpPost("{id:guid}/follow-up")]
    public async Task<IActionResult> FollowUp(Guid id, [FromBody] FollowUpEdit input)
    {
        var issue = await _db.IssueReports.Include(x => x.Company).Include(x => x.Recipients)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (issue == null) return NotFound();
        if (!User.CanReadIssue(issue)) return Forbid();
        // Companies may comment; status changes belong to managers and assigned organizations.
        if (input.NewStatus.HasValue && !User.IsManager() && User.IsInRole("Company"))
            return Forbid();
        if (issue.Status == IssueStatus.Closed && input.NewStatus != IssueStatus.UnderReview)
            return Conflict(new ApiMessage("پرونده بسته است؛ بازگشایی فقط توسط مدیر انجام می‌شود."));
        if (issue.Status == IssueStatus.Closed && !User.IsManager()) return Forbid();
        if (input.NewStatus.HasValue)
        {
            var next = input.NewStatus.Value;
            if (!AllowedTransition(issue.Status, next))
                return Conflict(new ApiMessage("تغییر وضعیت انتخاب‌شده با گردش‌کار سازگار نیست."));
            issue.Status = next;
        }
        _db.IssueFollowUps.Add(new IssueFollowUp { IssueReportId = id,
            ActorUserId = User.UserId()!, Comment = input.Comment.Trim(), Status = issue.Status });
        await _db.SaveChangesAsync();
        return Ok(new ApiMessage("پیگیری ثبت شد."));
    }
}
