using AbadanHub.Api.Data;
using AbadanHub.Api.Models;
using AbadanHub.Api.Security;
using AbadanHub.Shared;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly AppDbContext _db;
    public AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db)
    { _users = users; _signIn = signIn; _db = db; }

    [HttpGet("csrf")]
    public ActionResult<CsrfInfo> GetCsrf([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new CsrfInfo(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(new UserInfo(user.Id, user.Email!, user.DisplayName,
            (await _users.GetRolesAsync(user)).ToArray()));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthInput input)
    {
        var user = await _users.FindByEmailAsync(input.Email.Trim());
        if (user == null) return Unauthorized(new ApiMessage("ایمیل یا رمز عبور صحیح نیست."));
        var result = await _signIn.PasswordSignInAsync(user, input.Password, false, true);
        if (!result.Succeeded) return Unauthorized(new ApiMessage("ورود ناموفق است؛ حساب ممکن است موقتاً قفل شده باشد."));
        return Ok(new ApiMessage("ورود موفق بود."));
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new ApiMessage("خارج شدید."));
    }

    [HttpPost("register-company")]
    public async Task<IActionResult> RegisterCompany([FromBody] CompanyRegistration input)
    {
        if (await _users.FindByEmailAsync(input.Email.Trim()) != null)
            return Conflict(new ApiMessage("این ایمیل قبلاً ثبت شده است."));
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var user = new AppUser { UserName = input.Email.Trim(), Email = input.Email.Trim(),
            DisplayName = input.ManagerName.Trim() };
        var result = await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        var roleResult = await _users.AddToRoleAsync(user, "Company");
        if (!roleResult.Succeeded) return BadRequest(new { errors = roleResult.Errors.Select(x => x.Description) });
        _db.Companies.Add(new Company { OwnerId = user.Id, Name = input.CompanyName.Trim(),
            Sector = input.Sector.Trim(), RequestsTownshipVerification = input.RequestsTownshipVerification });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new ApiMessage("ثبت‌نام شرکت انجام شد. اطلاعات پس از تأیید منتشر می‌شود."));
    }

    [HttpPost("register-specialist")]
    public async Task<IActionResult> RegisterSpecialist([FromBody] SpecialistRegistration input)
    {
        if (await _users.FindByEmailAsync(input.Email.Trim()) != null)
            return Conflict(new ApiMessage("این ایمیل قبلاً ثبت شده است."));
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var user = new AppUser { UserName = input.Email.Trim(), Email = input.Email.Trim(),
            DisplayName = input.FullName.Trim() };
        var result = await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        var roleResult = await _users.AddToRoleAsync(user, "Specialist");
        if (!roleResult.Succeeded) return BadRequest(new { errors = roleResult.Errors.Select(x => x.Description) });
        _db.SpecialistProfiles.Add(new SpecialistProfile { UserId = user.Id,
            FullName = input.FullName.Trim(), City = input.City.Trim() });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new ApiMessage("ثبت‌نام متخصص انجام شد."));
    }
}
