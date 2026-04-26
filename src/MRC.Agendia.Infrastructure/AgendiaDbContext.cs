using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Infrastructure;

public class AgendiaDbContext : DbContext
{
    public AgendiaDbContext(DbContextOptions options) : base(options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Client)
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany()
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
            .HasIndex(h => new { h.Date, h.Scope, h.Region })
            .HasDatabaseName("IX_HolidayCalendar_Date_Scope_Region");

        modelBuilder.Entity<ScheduleOverride>()
            .HasIndex(so => new { so.BusinessId, so.Date })
            .IsUnique()
            .HasDatabaseName("IX_ScheduleOverride_BusinessId_Date");
    }
}
