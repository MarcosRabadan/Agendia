using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Infrastructure.Auditing;
using MRC.Agendia.Infrastructure.Authorization;
using MRC.Agendia.Infrastructure.Caching;
using MRC.Agendia.Infrastructure.Identity;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Infrastructure.Persistence;
using MRC.Agendia.Infrastructure.Repositories;
using MRC.Agendia.Infrastructure.Services;
using MRC.Agendia.Infrastructure.Time;

namespace MRC.Agendia.Infrastructure
{
    /// <summary>
    /// Single entry point to register the Infrastructure layer:
    /// DbContext, repositories, domain services, identity helpers,
    /// JWT and resource-based authorization.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Persistence (EF Core)
            services.AddScoped<AuditableSaveChangesInterceptor>();
            services.AddDbContext<AgendiaDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
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
            services.AddScoped<IClientRepository, ClientRepository>();
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
            services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Serializes the booking critical section per employee/day (anti double-booking)
            services.AddScoped<IBookingConcurrencyGuard, BookingConcurrencyGuard>();

            // Single app-wide business timezone for "now" comparisons in the booking flow
            services.AddSingleton<IClock, BusinessClock>();

            // Domain services
            services.AddScoped<IScheduleResolver, ScheduleResolver>();

            // Identity helpers (JWT and refresh tokens)
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
            services.AddScoped<IAuthResponseFactory, AuthResponseFactory>();
            services.AddScoped<IAuthEmailService, AuthEmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserRegistrationService, UserRegistrationService>();

            // Resource-based authorization (more setup in the API project because
            // it depends on IHttpContextAccessor; here just the infrastructural service)
            services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

            // Per-request multi-tenant business scope for the global query filter (#58).
            services.AddScoped<ICurrentBusinessScope, CurrentBusinessScope>();

            // Notifications (email; push/FCM tracked separately)
            services.AddScoped<INotificationService, NotificationService>();

            // Audit log
            services.AddScoped<IAuditLogger, AuditLogger>();

            // Hosted services
            services.AddHostedService<RefreshTokenCleanupService>();
            services.AddHostedService<AppointmentReminderService>();

            return services;
        }
    }
}
