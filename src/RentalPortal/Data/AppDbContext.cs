using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Entities;

namespace RentalPortal.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<RentalApplication> Applications => Set<RentalApplication>();
    public DbSet<Residence> Residences => Set<Residence>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<ApplicationStatusChange> StatusChanges => Set<ApplicationStatusChange>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UnitType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Property>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Address).HasMaxLength(200).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.State).HasMaxLength(50).IsRequired();
            e.Property(x => x.ZipCode).HasMaxLength(20).IsRequired();
        });

        builder.Entity<Unit>(e =>
        {
            e.Property(x => x.UnitNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.MonthlyRent).HasColumnType("decimal(12,2)");
            e.HasOne(x => x.Property)
                .WithMany(x => x.Units)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UnitType)
                .WithMany(x => x.Units)
                .HasForeignKey(x => x.UnitTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PropertyId, x.UnitNumber }).IsUnique();
        });

        builder.Entity<RentalApplication>(e =>
        {
            e.ToTable("Applications");
            e.Property(x => x.FullName).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(40);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.CurrentAddress).HasMaxLength(300);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ApplicantUserId);
            e.HasOne(x => x.Unit)
                .WithMany(x => x.Applications)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Applicant)
                .WithMany()
                .HasForeignKey(x => x.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Residence>(e =>
        {
            e.Property(x => x.Address).HasMaxLength(300).IsRequired();
            e.Property(x => x.LandlordName).HasMaxLength(120).IsRequired();
            e.Property(x => x.LandlordPhone).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Application)
                .WithMany(x => x.Residences)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Lease>(e =>
        {
            e.HasIndex(x => x.ApplicationId).IsUnique();
            e.HasIndex(x => new { x.UnitId, x.StartDate, x.EndDate });
            e.HasOne(x => x.Unit)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Application)
                .WithOne(x => x.Lease)
                .HasForeignKey<Lease>(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationStatusChange>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasOne(x => x.Application)
                .WithMany(x => x.StatusChanges)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
