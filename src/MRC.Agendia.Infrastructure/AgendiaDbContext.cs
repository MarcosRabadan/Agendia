using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Infrastructure;

// Users and credentials live in the Harmony identity service, so this context
// holds no identity tables: the *UserId columns store Harmony's opaque user id.
public class AgendiaDbContext : DbContext
{
    private readonly ICurrentBusinessScope _businessScope;

    public AgendiaDbContext(DbContextOptions<AgendiaDbContext> options, ICurrentBusinessScope businessScope) : base(options)
    {
        _businessScope = businessScope;
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

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Waitlist (#167)
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    // Multiservice (#170): extra services of an appointment beyond the primary one.
    public DbSet<AppointmentExtraService> AppointmentExtraServices => Set<AppointmentExtraService>();

    // Push device tokens (#51)
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

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

        // Indexes for fast user lookup
        modelBuilder.Entity<Business>()
            .HasIndex(b => b.OwnerUserId)
            .HasDatabaseName("IX_Business_OwnerUserId");

        // Notification language (es/en/fr). Column default backfills existing rows
        // when the migration adds the NOT NULL column.
        modelBuilder.Entity<Business>()
            .Property(b => b.DefaultLanguage)
            .HasMaxLength(10)
            .HasDefaultValue(SupportedLanguages.Spanish)
            .IsRequired();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Employee_UserId");

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Client_UserId");

        // Optional owning business for business-managed clients. No cascade: deleting
        // a business never deletes its clients (history is kept, like the other entities).
        modelBuilder.Entity<Client>()
            .HasOne<Business>()
            .WithMany()
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.BusinessId)
            .HasDatabaseName("IX_Client_BusinessId");

        // AuditLog
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLog_UserId");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLog_Action");

        // Soft delete: hide deleted rows from every query by default.
        // Restore paths use IgnoreQueryFilters() to reach them again.
        //
        // Multi-tenant business scope (#58, defense in depth over resource auth):
        // Business/Employee/Service additionally restrict to the caller's own
        // business(es) for Owner/Employee callers; the clause is a no-op for Admin,
        // anonymous and Client callers (see CurrentBusinessScope). Public reads of
        // Business/Service use IgnoreQueryFilters() so the catalog stays open to all.
        // NOT scoped here (deliberate): Appointment (no direct BusinessId; filtering
        // via Employee.BusinessId would fight the IgnoreQueryFilters reads that keep
        // appointments whose parent is soft-deleted, #127/#133), ScheduleTemplate/
        // ScheduleOverride (cross-tenant calendar reads are an accepted decision).
        // Those stay protected by resource authorization.
        modelBuilder.Entity<Business>().HasQueryFilter(b => !b.IsDeleted
            && (!_businessScope.IsRestricted || _businessScope.BusinessIds.Contains(b.Id)));
        modelBuilder.Entity<Client>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted
            && (!_businessScope.IsRestricted || _businessScope.BusinessIds.Contains(e.BusinessId)));
        modelBuilder.Entity<Service>().HasQueryFilter(s => !s.IsDeleted
            && (!_businessScope.IsRestricted || _businessScope.BusinessIds.Contains(s.BusinessId)));
        modelBuilder.Entity<Appointment>().HasQueryFilter(a => !a.IsDeleted);

        // Backfill CreatedAt for rows that existed before audit fields were added.
        // New rows get their value from AuditableSaveChangesInterceptor before insert.
        modelBuilder.Entity<Business>().Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Client>().Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Employee>().Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Service>().Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Appointment>().Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Index IsDeleted: every query now carries "WHERE IsDeleted = 0" from the
        // global filter above.
        modelBuilder.Entity<Business>().HasIndex(b => b.IsDeleted);
        modelBuilder.Entity<Client>().HasIndex(c => c.IsDeleted);
        modelBuilder.Entity<Employee>().HasIndex(e => e.IsDeleted);
        modelBuilder.Entity<Service>().HasIndex(s => s.IsDeleted);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.IsDeleted);

        // The reminder job and the date-range reads filter appointments by StartDate.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.StartDate)
            .HasDatabaseName("IX_Appointment_StartDate");

        // Series operations (cancel/move/delete) look up all appointments of a
        // recurring series by SeriesId. Filtered index: one-off appointments are null.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.SeriesId)
            .HasDatabaseName("IX_Appointment_SeriesId")
            .HasFilter("[SeriesId] IS NOT NULL");

        // Waitlist (#167)
        modelBuilder.Entity<WaitlistEntry>()
            .HasOne(w => w.Client)
            .WithMany()
            .HasForeignKey(w => w.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WaitlistEntry>()
            .HasOne(w => w.Service)
            .WithMany()
            .HasForeignKey(w => w.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // BusinessId has no navigation on WaitlistEntry; configure the FK explicitly so
        // the column is a real relationship (referential integrity + index). Restrict
        // like the other waitlist FKs: Business is soft-deleted, never cascaded.
        modelBuilder.Entity<WaitlistEntry>()
            .HasOne<Business>()
            .WithMany()
            .HasForeignKey(w => w.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WaitlistEntry>()
            .Property(w => w.Status)
            .HasConversion<int>();

        // The freed-slot trigger looks up waiting entries by this exact tuple.
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(w => new { w.BusinessId, w.ServiceId, w.Date, w.StartTime, w.Status })
            .HasDatabaseName("IX_WaitlistEntry_Slot");

        // Dedup active waitlist entries at the DB level: JoinAsync is check-then-insert
        // and can race. Filtered to Waiting (Status = 0) so a client can re-join after
        // leaving (Cancelled) or being Notified. SQL Server treats equal NULLs as a
        // duplicate, so "any employee" (EmployeeId NULL) entries are deduped per slot too.
        // ClientId is placed last (the index is filtered, so leading with it would make
        // EF drop the standalone FK index on ClientId that other reads still rely on).
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(w => new { w.BusinessId, w.ServiceId, w.Date, w.StartTime, w.EmployeeId, w.ClientId })
            .IsUnique()
            .HasFilter("[Status] = 0")
            .HasDatabaseName("IX_WaitlistEntry_UniqueWaiting");

        // Multiservice (#170): an appointment may include extra services beyond the
        // primary ServiceId. Cascade from the appointment (owned children); restrict
        // on Service so deleting/soft-deleting a service never cascades into bookings.
        modelBuilder.Entity<AppointmentExtraService>()
            .HasOne(x => x.Appointment)
            .WithMany(a => a.ExtraServices)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppointmentExtraService>()
            .HasOne(x => x.Service)
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentExtraService>()
            .HasIndex(x => x.AppointmentId)
            .HasDatabaseName("IX_AppointmentExtraService_AppointmentId");

        // Push device tokens (#51): one row per token (unique), fanned out by user.
        modelBuilder.Entity<DeviceToken>()
            .Property(d => d.Platform)
            .HasConversion<int>();

        modelBuilder.Entity<DeviceToken>()
            .HasIndex(d => d.Token)
            .IsUnique()
            .HasDatabaseName("IX_DeviceToken_Token");

        modelBuilder.Entity<DeviceToken>()
            .HasIndex(d => d.UserId)
            .HasDatabaseName("IX_DeviceToken_UserId");
    }
}
