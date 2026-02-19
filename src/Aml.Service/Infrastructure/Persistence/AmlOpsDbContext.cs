using AmlOps.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AmlOps.Backend.Infrastructure.Persistence;

public sealed class AmlOpsDbContext(DbContextOptions<AmlOpsDbContext> options) : DbContext(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CaseEvent> CaseEvents => Set<CaseEvent>();
    public DbSet<CaseComment> CaseComments => Set<CaseComment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ImportedAlert> ImportedAlerts => Set<ImportedAlert>();
    public DbSet<SlaSettings> SlaSettings => Set<SlaSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("aml");

        modelBuilder.Entity<Case>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.RiskLevel).HasConversion<string>();
            entity.HasIndex(x => new { x.TenantId, x.CaseNumber }).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
            entity.Property(x => x.IdentifiersJson).HasColumnType("TEXT");
            entity.Property(x => x.RiskFlagsJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<CaseEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CaseId, x.At });
            entity.Property(x => x.PayloadJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<CaseComment>(entity => entity.HasKey(x => x.Id));

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TagsJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<ImportedAlert>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ExternalAlertId }).IsUnique();
        });

        modelBuilder.Entity<SlaSettings>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TenantId).IsUnique();
        });
    }
}
