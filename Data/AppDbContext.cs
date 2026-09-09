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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
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
    }
}