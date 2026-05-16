using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Infrastructure.Identity;

namespace MRC.Agendia.Infrastructure;

public class AgendiaDbContext : IdentityDbContext<ApplicationUser>
{
    public AgendiaDbContext(DbContextOptions<AgendiaDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    // Schedule system
    public DbSet<ScheduleTemplate> ScheduleTemplates => Set<ScheduleTemplate>();
    public DbSet<WeeklyTimeSlot> WeeklyTimeSlots => Set<WeeklyTimeSlot>();
    public DbSet<ScheduleOverride> ScheduleOverrides => Set<ScheduleOverride>();
    public DbSet<CustomTimeSlot> CustomTimeSlots => Set<CustomTimeSlot>();
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();

    // Auth
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Client)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.Appointments)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ScheduleTemplate
        modelBuilder.Entity<ScheduleTemplate>()
            .HasOne(st => st.Business)
            .WithMany(b => b.ScheduleTemplates)
            .HasForeignKey(st => st.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduleTemplate>()
            .HasMany(st => st.WeeklySlots)
            .WithOne(ws => ws.ScheduleTemplate)
            .HasForeignKey(ws => ws.ScheduleTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WeeklyTimeSlot>()
            .Property(ws => ws.DayOfWeek)
            .HasConversion<int>();

        // ScheduleOverride
        modelBuilder.Entity<ScheduleOverride>()
            .HasOne(so => so.Business)
            .WithMany(b => b.ScheduleOverrides)
            .HasForeignKey(so => so.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduleOverride>()
            .HasMany(so => so.CustomSlots)
            .WithOne(cs => cs.ScheduleOverride)
            .HasForeignKey(cs => cs.ScheduleOverrideId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduleOverride>()
            .Property(so => so.OverrideType)
            .HasConversion<int>();

        // HolidayCalendar
        modelBuilder.Entity<HolidayCalendar>()
            .Property(h => h.Scope)
            .HasConversion<int>();

        modelBuilder.Entity<HolidayCalendar>()
            .HasIndex(h => new { h.Date, h.Scope })
            .HasDatabaseName("IX_HolidayCalendar_Date_Scope");

        modelBuilder.Entity<ScheduleOverride>()
            .HasIndex(so => new { so.BusinessId, so.Date })
            .IsUnique()
            .HasDatabaseName("IX_ScheduleOverride_BusinessId_Date");

        // RefreshToken
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshToken_Token");

        // Indexes for fast user lookup
        modelBuilder.Entity<Business>()
            .HasIndex(b => b.OwnerUserId)
            .HasDatabaseName("IX_Business_OwnerUserId");

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Employee_UserId");

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Client_UserId");
    }
}
