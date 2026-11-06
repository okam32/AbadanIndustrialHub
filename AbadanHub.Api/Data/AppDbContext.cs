using AbadanHub.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AbadanHub.Api.Data;

public sealed class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyPublication> CompanyPublications => Set<CompanyPublication>();
    public DbSet<Offering> Offerings => Set<Offering>();
    public DbSet<OfferingPublication> OfferingPublications => Set<OfferingPublication>();
    public DbSet<SpecialistProfile> SpecialistProfiles => Set<SpecialistProfile>();
    public DbSet<SpecialistSkill> SpecialistSkills => Set<SpecialistSkill>();
    public DbSet<JobVacancy> JobVacancies => Set<JobVacancy>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<WorkProcess> WorkProcesses => Set<WorkProcess>();
    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();
    public DbSet<IssueReport> IssueReports => Set<IssueReport>();
    public DbSet<IssueRecipient> IssueRecipients => Set<IssueRecipient>();
    public DbSet<IssueFollowUp> IssueFollowUps => Set<IssueFollowUp>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<CompanyPublication>().HasKey(x => x.CompanyId);
        m.Entity<OfferingPublication>().HasKey(x => x.OfferingId);
        m.Entity<Company>().HasIndex(x => x.OwnerId).IsUnique();
        m.Entity<Company>().HasOne(x => x.Publication).WithOne(x => x.Company)
            .HasForeignKey<CompanyPublication>(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Offering>().HasOne(x => x.Publication).WithOne(x => x.Offering)
            .HasForeignKey<OfferingPublication>(x => x.OfferingId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Offering>().HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<SpecialistProfile>().HasIndex(x => x.UserId).IsUnique();
        m.Entity<JobApplication>().HasIndex(x => new { x.VacancyId, x.SpecialistProfileId }).IsUnique();
        m.Entity<JobApplication>().HasOne(x => x.SpecialistProfile).WithMany()
            .HasForeignKey(x => x.SpecialistProfileId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<IssueRecipient>().HasKey(x => new { x.IssueReportId, x.Organization });
        m.Entity<IssueReport>().HasOne(x => x.WorkProcess).WithMany()
            .HasForeignKey(x => x.WorkProcessId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<IssueFollowUp>().HasOne(x => x.Actor).WithMany()
            .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<JobVacancy>().HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<WorkProcess>().HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<IssueReport>().HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ProcessStep>().HasOne(x => x.WorkProcess).WithMany(x => x.Steps)
            .HasForeignKey(x => x.WorkProcessId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<IssueRecipient>().HasOne(x => x.IssueReport).WithMany(x => x.Recipients)
            .HasForeignKey(x => x.IssueReportId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<IssueFollowUp>().HasOne(x => x.IssueReport).WithMany(x => x.FollowUps)
            .HasForeignKey(x => x.IssueReportId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<StoredFile>().HasOne(x => x.IssueReport).WithMany()
            .HasForeignKey(x => x.IssueReportId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<StoredFile>().HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Company>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        m.Entity<Offering>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        m.Entity<Offering>().Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        m.Entity<OfferingPublication>().Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        m.Entity<SpecialistSkill>().Property(x => x.Level).HasConversion<string>().HasMaxLength(30);
        m.Entity<IssueReport>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        m.Entity<IssueReport>().Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
        m.Entity<IssueRecipient>().Property(x => x.Organization).HasConversion<string>().HasMaxLength(40);
        m.Entity<IssueFollowUp>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        foreach (var entity in m.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.GetMaxLength() == null &&
                    !property.IsPrimaryKey() && !property.IsForeignKey())
                {
                    property.SetMaxLength(4000);
                }
            }
        }
    }
}
