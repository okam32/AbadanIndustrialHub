using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/files"), Authorize]
public sealed class FilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    public FilesController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    { _db = db; _env = env; _config = config; }

    private string StorageRoot => Path.GetFullPath(_config["Storage:Root"] ??
        Path.Combine(_env.ContentRootPath, "private_uploads"));

    [HttpPost("upload"), Authorize(Roles = "Company")]
    [RequestSizeLimit(5_300_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] Guid? issueId = null)
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        if (file == null || file.Length < 8 || file.Length > 5_000_000)
            return BadRequest(new ApiMessage("حجم فایل باید بین ۱ بایت و ۵ مگابایت باشد."));
        if (issueId.HasValue && !await _db.IssueReports.AnyAsync(x =>
            x.Id == issueId && x.CompanyId == company.Id)) return Forbid();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".png" or ".jpg" or ".jpeg"))
            return BadRequest(new ApiMessage("فقط PDF، PNG و JPG مجاز است."));
        // Check signatures, never rely on client-provided file MIME types.
        var prefix = new byte[8];
        await using (var stream = file.OpenReadStream())
            await stream.ReadExactlyAsync(prefix);
        var valid = extension switch
        {
            ".pdf" => prefix.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".png" => prefix.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF
        };
        if (!valid) return BadRequest(new ApiMessage("امضای فایل با پسوند آن همخوانی ندارد."));
        var contentType = extension switch
        {
            ".pdf" => "application/pdf", ".png" => "image/png", _ => "image/jpeg"
        };
        Directory.CreateDirectory(StorageRoot);
        var storedName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(StorageRoot, storedName);
        try
        {
            await using (var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true))
                await file.CopyToAsync(destination, HttpContext.RequestAborted);
            var entity = new StoredFile { CompanyId = company.Id, IssueReportId = issueId,
                StoredName = storedName, OriginalName = Path.GetFileName(file.FileName),
                ContentType = contentType, Length = file.Length };
            _db.StoredFiles.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(new { entity.Id, message = "فایل با موفقیت بارگذاری شد." });
        }
        catch
        {
            System.IO.File.Delete(fullPath);
            throw;
        }
    }

    [HttpGet("mine"), Authorize(Roles = "Company")]
    public async Task<IActionResult> Mine()
    {
        var company = await _db.MyCompanyAsync(User);
        if (company == null) return NotFound();
        return Ok(await _db.StoredFiles.AsNoTracking().Where(x => x.CompanyId == company.Id)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new { x.Id, x.OriginalName, x.Length, x.CreatedUtc }).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var f = await _db.StoredFiles.Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound();
        var allowed = User.IsManager() || f.Company.OwnerId == User.UserId();
        if (!allowed && f.IssueReportId.HasValue)
        {
            var issue = await _db.IssueReports.Include(x => x.Company).Include(x => x.Recipients)
                .FirstOrDefaultAsync(x => x.Id == f.IssueReportId.Value);
            allowed = issue != null && User.CanReadIssue(issue);
        }
        if (!allowed) return Forbid();
        var fullPath = Path.Combine(StorageRoot, f.StoredName);
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        return PhysicalFile(fullPath, f.ContentType, f.OriginalName);
    }
}
