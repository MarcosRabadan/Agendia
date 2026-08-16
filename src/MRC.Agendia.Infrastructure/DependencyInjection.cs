using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Idempotency;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Infrastructure.Auditing;
using MRC.Agendia.Infrastructure.Authorization;
using MRC.Agendia.Infrastructure.Caching;
using MRC.Agendia.Infrastructure.Idempotency;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Infrastructure.Persistence;
using MRC.Agendia.Infrastructure.Repositories;
using MRC.Agendia.Infrastructure.ServiceAuth;
using MRC.Agendia.Infrastructure.Services;
using MRC.Agendia.Infrastructure.Time;
using MRC.Agendia.Application.ServiceAuth;

namespace MRC.Agendia.Infrastructure
{
    /// <summary>
    /// Single entry point to register the Infrastructure layer:
    /// DbContext, repositories, domain services and resource-based authorization.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Persistence (EF Core)
            services.AddScoped<AuditableSaveChangesInterceptor>();
            services.AddDbContext<AgendiaDbContext>((sp, options) =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>())
                       // Business has a soft-delete query filter while its schedule
                       // children (ScheduleTemplate/Override) intentionally do not.
                       // Schedule queries never traverse the Business navigation
                       // (they filter by the BusinessId scalar), so the interaction
                       // warning describes a path this codebase never takes.
                       .ConfigureWarnings(w => w.Ignore(
                           CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

            // In-memory cache for read-heavy, rarely-changing data (#55).
            services.AddMemoryCache();

            // Repositories
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IScheduleOverrideRepository, ScheduleOverrideRepository>();

            // Schedule templates + holidays are decorated with a caching layer (#55):
            // register the concrete repo, then wrap it with the caching decorator.
            services.AddScoped<ScheduleTemplateRepository>();
            services.AddScoped<IScheduleTemplateRepository>(sp => new CachingScheduleTemplateRepository(
                sp.GetRequiredService<ScheduleTemplateRepository>(), sp.GetRequiredService<IMemoryCache>()));
            services.AddScoped<HolidayCalendarRepository>();
            services.AddScoped<IHolidayCalendarRepository>(sp => new CachingHolidayCalendarRepository(
                sp.GetRequiredService<HolidayCalendarRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<AgendiaDbContext>()));
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IBusinessStatsRepository, BusinessStatsRepository>();
            services.AddScoped<IWaitlistRepository, WaitlistRepository>();
            services.AddScoped<IEmployeeTimeOffRepository, EmployeeTimeOffRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Serializes the booking critical section per employee/day (anti double-booking)
            services.AddScoped<IBookingConcurrencyGuard, BookingConcurrencyGuard>();

            // Single app-wide business timezone for "now" comparisons in the booking flow
            services.AddSingleton<IClock, BusinessClock>();

            // Domain services
            services.AddScoped<IScheduleResolver, ScheduleResolver>();

            // Resource-based authorization (more setup in the API project because
            // it depends on IHttpContextAccessor; here just the infrastructural service)
            services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

            // Per-request multi-tenant business scope for the global query filter (#58).
            services.AddScoped<ICurrentBusinessScope, CurrentBusinessScope>();

            // Notifications by domain events (#246): Agendia no longer delivers email/push.
            // Entities raise integration events (see AuditableEntity) that the DbContext's
            // SaveChanges override enlists into a transactional outbox; the dispatcher hands
            // them to a swappable transport (log-only until the system-wide broker is chosen -
            // RabbitMQ/Azure SB/Kafka).
            services.AddScoped<IEventTransport, LoggingEventTransport>();
            services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
            services.AddScoped<OutboxProcessor>();

            // Reminder job (24h): publishes AppointmentReminder events. The processor takes a
            // session-level advisory lock so only one instance runs the batch (N-instance safe).
            services.Configure<ReminderOptions>(configuration.GetSection(ReminderOptions.SectionName));
            services.AddScoped<ReminderProcessor>();

            // Waitlist priority hold (#268): options bound here and exposed as a plain
            // instance so the Application layer needs no configuration package.
            services.Configure<WaitlistOptions>(configuration.GetSection(WaitlistOptions.SectionName));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<WaitlistOptions>>().Value);
            services.AddScoped<WaitlistHoldProcessor>();

            // Idempotent booking (#266): durable record of the requests already served
            // under an Idempotency-Key, plus the purge of the expired ones.
            services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();

            // Audit log
            services.AddScoped<IAuditLogger, AuditLogger>();

            // Machine-to-machine (client-credentials) service auth (#232).
            // Trusted clients live in the "ServiceClients" config array (secrets are
            // hashed); the issuer signs with the same key Harmony's tokens validate with.
            services.Configure<ServiceClientRegistryOptions>(configuration);
            services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));
            services.AddScoped<IServiceClientAuthenticator, ConfigurationServiceClientAuthenticator>();
            services.AddScoped<IServiceTokenIssuer, JwtServiceTokenIssuer>();

            // Hosted services
            services.AddHostedService<AppointmentReminderService>();
            services.AddHostedService<OutboxDispatcherService>();
            services.AddHostedService<IdempotencyPurgeService>();
            services.AddHostedService<WaitlistHoldExpiryService>();

            return services;
        }
    }
}
