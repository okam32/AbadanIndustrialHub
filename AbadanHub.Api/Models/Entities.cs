using AbadanHub.Shared;
using Microsoft.AspNetCore.Identity;

namespace AbadanHub.Api.Models;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public AppUser Owner { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public string City { get; set; } = "آبادان";
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? NationalIdentifier { get; set; }
    public string? Address { get; set; }
    public bool RequestsTownshipVerification { get; set; }
    public bool IsTownshipVerified { get; set; }
    public bool IsArchived { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? ReviewNote { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public CompanyPublication? Publication { get; set; }
}
public sealed class CompanyPublication
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public string City { get; set; } = "";
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public bool IsTownshipVerified { get; set; }
    public DateTime PublishedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class Offering
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public OfferingKind Kind { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? ReviewNote { get; set; }
    public bool IsArchived { get; set; }
    public OfferingPublication? Publication { get; set; }
}
public sealed class OfferingPublication
{
    public Guid OfferingId { get; set; }
    public Offering Offering { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public OfferingKind Kind { get; set; }
    public DateTime PublishedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class SpecialistProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string City { get; set; } = "آبادان";
    public string? Bio { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<SpecialistSkill> Skills { get; set; } = new();
}
public sealed class SpecialistSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpecialistProfileId { get; set; }
    public SpecialistProfile SpecialistProfile { get; set; } = null!;
    public string Name { get; set; } = "";
    public SkillLevel Level { get; set; }
    public int YearsExperience { get; set; }
}
public sealed class JobVacancy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string RequiredSkills { get; set; } = "";
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VacancyId { get; set; }
    public JobVacancy Vacancy { get; set; } = null!;
    public Guid SpecialistProfileId { get; set; }
    public SpecialistProfile SpecialistProfile { get; set; } = null!;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class WorkProcess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public List<ProcessStep> Steps { get; set; } = new();
}
public sealed class ProcessStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkProcessId { get; set; }
    public WorkProcess WorkProcess { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Title { get; set; } = "";
}
public sealed class IssueReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? WorkProcessId { get; set; }
    public WorkProcess? WorkProcess { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public IssuePriority Priority { get; set; } = IssuePriority.Normal;
    public IssueStatus Status { get; set; } = IssueStatus.Submitted;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<IssueRecipient> Recipients { get; set; } = new();
    public List<IssueFollowUp> FollowUps { get; set; } = new();
}
public sealed class IssueRecipient
{
    public Guid IssueReportId { get; set; }
    public IssueReport IssueReport { get; set; } = null!;
    public OrganizationKind Organization { get; set; }
}
public sealed class IssueFollowUp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssueReportId { get; set; }
    public IssueReport IssueReport { get; set; } = null!;
    public string ActorUserId { get; set; } = "";
    public AppUser Actor { get; set; } = null!;
    public string Comment { get; set; } = "";
    public IssueStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActorUserId { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetId { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? IssueReportId { get; set; }
    public IssueReport? IssueReport { get; set; }
    public string StoredName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
