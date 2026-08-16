using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Infrastructure.Idempotency;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Infrastructure.Persistence;

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

    // The JSON shape of an outbox payload (camelCase, Web defaults) - the contract the
    // downstream consumer reads (see docs/events-contract.md).
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnlistDomainEvents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnlistDomainEvents();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Converts the integration events raised by the tracked entities into OutboxMessages and
    // adds them to this same change set BEFORE the base save runs, so they commit in the same
    // transaction as the state change that raised them (transactional outbox). This is a
    // SaveChanges override rather than a SavingChanges interceptor on purpose: entities added
    // from inside that interceptor are not reliably included in the same save.
    private void EnlistDomainEvents()
    {
        var entities = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    // Serialize against the runtime type so the concrete event's properties
                    // are written, not just the marker interface.
                    Type = domainEvent.GetType().Name,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), OutboxSerializerOptions),
                    OccurredOnUtc = domainEvent.OccurredOnUtc,
                });
            }

            entity.ClearDomainEvents();
        }
    }

    public DbSet<Business> Businesses => Set<Business>();
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

    // Transactional outbox (#246): integration events pending delivery.
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Requests already served under an Idempotency-Key (#266).</summary>
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The booking client is a Harmony user id (the JWT sub), not a local entity:
        // stored as a plain scalar column with an index, no cross-service FK.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.ClientUserId)
            .HasDatabaseName("IX_Appointment_ClientUserId");

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
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted
            && (!_businessScope.IsRestricted || _businessScope.BusinessIds.Contains(e.BusinessId)));
        modelBuilder.Entity<Service>().HasQueryFilter(s => !s.IsDeleted
            && (!_businessScope.IsRestricted || _businessScope.BusinessIds.Contains(s.BusinessId)));
        modelBuilder.Entity<Appointment>().HasQueryFilter(a => !a.IsDeleted);

        // Backfill CreatedAt for rows that existed before audit fields were added.
        // New rows get their value from AuditableSaveChangesInterceptor before insert.
        modelBuilder.Entity<Business>().Property(b => b.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Employee>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Service>().Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Appointment>().Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        // Index IsDeleted: every query now carries "WHERE IsDeleted = 0" from the
        // global filter above.
        modelBuilder.Entity<Business>().HasIndex(b => b.IsDeleted);
        modelBuilder.Entity<Employee>().HasIndex(e => e.IsDeleted);
        modelBuilder.Entity<Service>().HasIndex(s => s.IsDeleted);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.IsDeleted);

        // StartDate/EndDate are WALL-CLOCK times (DateTime with Kind=Unspecified, as
        // they arrive from JSON and as BusinessClock.BusinessNow returns them), not UTC
        // instants. They must map to `timestamp without time zone`: Npgsql refuses to
        // write a non-UTC DateTime to `timestamp with time zone` (throws), which would
        // 500 every appointment create/move on real PostgreSQL. The genuinely-UTC
        // columns (CreatedAt/UpdatedAt/DeletedAt, ReminderSentAt, AuditLog.Timestamp,
        // OutboxMessage.*OnUtc) are UTC instants and correctly stay timestamptz.
        modelBuilder.Entity<Appointment>().Property(a => a.StartDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Appointment>().Property(a => a.EndDate).HasColumnType("timestamp without time zone");

        // The reminder job and the date-range reads filter appointments by StartDate.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.StartDate)
            .HasDatabaseName("IX_Appointment_StartDate");

        // Series operations (cancel/move/delete) look up all appointments of a
        // recurring series by SeriesId. Filtered index: one-off appointments are null.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.SeriesId)
            .HasDatabaseName("IX_Appointment_SeriesId")
            .HasFilter("\"SeriesId\" IS NOT NULL");

        // Waitlist (#167). The waiting client is a Harmony user id (no local entity),
        // stored as a scalar column with an index for the "my entries" lookup.
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(w => w.ClientUserId)
            .HasDatabaseName("IX_WaitlistEntry_ClientUserId");

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
        // leaving (Cancelled) or being Notified. AreNullsDistinct(false) => NULLS NOT
        // DISTINCT, so an "any employee" (EmployeeId NULL) entry is deduped per slot too:
        // Postgres treats NULLs as distinct by default (unlike SQL Server), which would
        // otherwise let duplicate "any employee" waitings through.
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(w => new { w.BusinessId, w.ServiceId, w.Date, w.StartTime, w.EmployeeId, w.ClientUserId })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("\"Status\" = 0")
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

        // Transactional outbox (#246): events written with the operation that
        // raised them and delivered later by OutboxDispatcherService.
        modelBuilder.Entity<OutboxMessage>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<OutboxMessage>()
            .Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<OutboxMessage>()
            .Property(m => m.Payload)
            .IsRequired();

        // The dispatcher polls pending rows (ProcessedOnUtc IS NULL) oldest first;
        // a filtered index keeps that scan cheap as processed rows accumulate.
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => m.OccurredOnUtc)
            .HasFilter("\"ProcessedOnUtc\" IS NULL")
            .HasDatabaseName("IX_OutboxMessage_Pending");

        // Idempotency records (#266): the key IS the primary key, so a concurrent twin
        // request loses the INSERT instead of booking a second appointment.
        modelBuilder.Entity<IdempotencyRecord>()
            .HasKey(r => r.Key);

        modelBuilder.Entity<IdempotencyRecord>()
            .Property(r => r.Key)
            .HasMaxLength(IdempotencyLimits.MaxStorageKeyLength);

        modelBuilder.Entity<IdempotencyRecord>()
            .Property(r => r.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        // The purge scans by age; keeping it indexed stops the table from being swept
        // whole once the traffic piles rows up.
        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(r => r.CreatedAtUtc)
            .HasDatabaseName("IX_IdempotencyRecord_CreatedAtUtc");

        // Guid primary keys are generated as sequential UUIDv7 client-side, at Add
        // time (not by the database), so ids are known before SaveChanges - two
        // just-added entities never collide on Guid.Empty. EF skips the generator
        // when a value is already set, so provisioned projections keep their external
        // Guid. The DB column has no default (the value always arrives from the app).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is null) continue;

            foreach (var property in primaryKey.Properties)
            {
                if (property.ClrType == typeof(Guid))
                    modelBuilder.Entity(entityType.ClrType).Property(property.Name)
                        .HasValueGenerator(typeof(UuidV7ValueGenerator))
                        .ValueGeneratedOnAdd();
            }
        }
    }
}
