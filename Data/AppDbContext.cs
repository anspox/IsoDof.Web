using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Dof> Dofs => Set<Dof>();
    public DbSet<DofAction> DofActions => Set<DofAction>();
    public DbSet<DofComment> DofComments { get; set; }
    public DbSet<DofAttachment> DofAttachments { get; set; }
    public DbSet<DofStatusHistory> DofStatusHistories => Set<DofStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.SicilNo)
            .IsUnique()
            .HasFilter("[SicilNo] IS NOT NULL");

        modelBuilder.Entity<Department>()
            .HasOne(d => d.QualityResponsibleUser)
            .WithMany()
            .HasForeignKey(d => d.QualityResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Dof>()
            .HasOne(d => d.Department)
            .WithMany(dept => dept.Dofs)
            .HasForeignKey(d => d.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Dof>()
            .HasOne(d => d.CreatedByUser)
            .WithMany(u => u.CreatedDofs)
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Dof>()
            .HasOne(d => d.AssignedToUser)
            .WithMany(u => u.AssignedDofs)
            .HasForeignKey(d => d.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DofComment>()
            .HasOne(c => c.Dof)
            .WithMany(d => d.Comments)
            .HasForeignKey(c => c.DofId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DofComment>()
            .HasOne(c => c.AuthorUser)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<DofAttachment>()
            .HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DofStatusHistory>()
            .HasOne(h => h.Dof)
            .WithMany(d => d.StatusHistory)
            .HasForeignKey(h => h.DofId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DofStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Dof>()
            .HasOne(d => d.ArchivedByUser)
            .WithMany()
            .HasForeignKey(d => d.ArchivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientUserId, n.IsRead });
    }
}