using Microsoft.EntityFrameworkCore;
using Trackly.Models;

namespace Trackly.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Timeplan> Timeplans { get; set; }
    public DbSet<TimePlanItem> TimePlanItems { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<MainAdmin> MainAdmins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Timeplan>()
            .HasOne(t => t.Employee)
            .WithMany(e => e.Timeplans)
            .HasForeignKey(t => t.EmployeeId);

        modelBuilder.Entity<Timeplan>()
            .HasMany(t => t.Items)
            .WithOne(i => i.Timeplan)
            .HasForeignKey(i => i.TimeplanId);

         modelBuilder.Entity<Admin>()
        .HasOne(a => a.Department)
        .WithOne(d => d.Admin)
        .HasForeignKey<Admin>(a => a.DepartmentId);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId);
    }
}


