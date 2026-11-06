using System.ComponentModel.DataAnnotations;

namespace AbadanHub.Shared;

public enum ApprovalStatus { Draft, Pending, Approved, Rejected }
public enum OfferingKind { Product, Service }
public enum SkillLevel { Beginner, Intermediate, Advanced, Expert }
public enum IssueStatus { Submitted, UnderReview, Referred, InProgress, Resolved, Closed }
public enum IssuePriority { Low, Normal, High, Critical }
public enum OrganizationKind { ChamberOfCommerce, AbadanRefinery, EngineersBasij }

public sealed class AuthInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public sealed class SpecialistRegistration : AuthInputBase
{
    [Required, StringLength(110)] public string FullName { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "آبادان";
}
public abstract class AuthInputBase
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(10)] public string Password { get; set; } = "";
}
public sealed class CompanyRegistration : AuthInputBase
{
    [Required, StringLength(180)] public string CompanyName { get; set; } = "";
    [Required, StringLength(110)] public string ManagerName { get; set; } = "";
    [Required, StringLength(150)] public string Sector { get; set; } = "";
    public bool RequestsTownshipVerification { get; set; }
}
public record UserInfo(string Id, string Email, string DisplayName, string[] Roles);
public record CsrfInfo(string Token);
public record CompanySummary(Guid Id, string Name, string Sector, string City, bool IsTownshipVerified,
    string? Description, string? Phone);
public sealed class CompanyEdit
{
    [Required, StringLength(180)] public string Name { get; set; } = "";
    [Required, StringLength(150)] public string Sector { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "آبادان";
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(30)] public string? NationalIdentifier { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    public bool RequestsTownshipVerification { get; set; }
}
public record CompanyMine(Guid Id, CompanyEdit Draft, ApprovalStatus Status, bool IsTownshipVerified, string? ReviewNote);
public sealed class OfferingEdit
{
    public Guid? Id { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = "";
    [Required, StringLength(4000)] public string Description { get; set; } = "";
    public OfferingKind Kind { get; set; } = OfferingKind.Service;
}
public record OfferingView(Guid Id, Guid CompanyId, string CompanyName, string Title, string Description, OfferingKind Kind);
public sealed class SkillEdit
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public SkillLevel Level { get; set; } = SkillLevel.Intermediate;
    [Range(0, 60)] public int YearsExperience { get; set; }
}
public sealed class SpecialistEdit
{
    [Required, StringLength(110)] public string FullName { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "آبادان";
    [StringLength(4000)] public string? Bio { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<SkillEdit> Skills { get; set; } = new();
}
public record SpecialistView(Guid Id, string FullName, string City, string? Bio, List<SkillEdit> Skills);
public sealed class JobEdit
{
    [Required, StringLength(150)] public string Title { get; set; } = "";
    [Required, StringLength(4000)] public string Description { get; set; } = "";
    [Required, StringLength(300)] public string RequiredSkills { get; set; } = "";
}
public record JobView(Guid Id, string CompanyName, string Title, string Description, string RequiredSkills, DateTime CreatedUtc);
public sealed class ProcessEdit
{
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
    public List<string> Steps { get; set; } = new();
}
public record ProcessView(Guid Id, string Title, string? Description, List<string> Steps);
public sealed class IssueEdit
{
    [Required, StringLength(180)] public string Title { get; set; } = "";
    [Required, StringLength(4000)] public string Description { get; set; } = "";
    [Required, StringLength(120)] public string Category { get; set; } = "";
    public IssuePriority Priority { get; set; } = IssuePriority.Normal;
    public Guid? WorkProcessId { get; set; }
    public List<OrganizationKind> Recipients { get; set; } = new();
}
public record IssueView(Guid Id, string CompanyName, string Title, string Description, string Category,
    IssuePriority Priority, IssueStatus Status, List<OrganizationKind> Recipients, DateTime CreatedUtc);
public sealed class FollowUpEdit
{
    [Required, StringLength(3000)] public string Comment { get; set; } = "";
    public IssueStatus? NewStatus { get; set; }
}
public record IssueFollowUpView(string Actor, string Comment, IssueStatus Status, DateTime CreatedUtc);
public record IssueDetail(IssueView Issue, List<IssueFollowUpView> FollowUps);
public sealed class ApprovalDecision
{
    public bool VerifyTownship { get; set; }
    [StringLength(1000)] public string? Reason { get; set; }
}
public record PendingCompany(Guid Id, string Name, string Sector, bool RequestedTownshipVerification, DateTime CreatedUtc);
public record PendingOffering(Guid Id, string Title, string CompanyName);
public record ReportSlice(string Name, int Count);
public record DashboardReport(int TotalCompanies, int VerifiedTownshipCompanies, int Specialists, int OpenJobs,
    int OpenIssues, List<ReportSlice> CompaniesBySector, List<ReportSlice> SpecialistsByCity,
    List<ReportSlice> SkillsByLevel, List<ReportSlice> IssuesByStatus);
public record ApiMessage(string Message);
