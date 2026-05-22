using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Infrastructure.Authorization;
using MRC.Agendia.Infrastructure.Identity;
using MRC.Agendia.Infrastructure.Repositories;
using MRC.Agendia.Infrastructure.Services;

namespace MRC.Agendia.Infrastructure
{
    /// <summary>
    /// Punto de entrada unico para registrar la capa Infrastructure:
    /// DbContext, repositorios, servicios de dominio, identity helpers,
    /// JWT y resource-based authorization.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Persistencia (EF Core)
            services.AddDbContext<AgendiaDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositorios
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IScheduleTemplateRepository, ScheduleTemplateRepository>();
            services.AddScoped<IScheduleOverrideRepository, ScheduleOverrideRepository>();
            services.AddScoped<IHolidayCalendarRepository, HolidayCalendarRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Domain services
            services.AddScoped<IScheduleResolver, ScheduleResolver>();

            // Identity helpers (JWT y refresh tokens)
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
            services.AddScoped<IAuthResponseFactory, AuthResponseFactory>();
            services.AddScoped<IAuthEmailService, AuthEmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserRegistrationService, UserRegistrationService>();

            // Resource-based authorization (mas configuracion en API porque
            // depende de IHttpContextAccessor; aqui solo el servicio infraestructural)
            services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

            // Hosted services
            services.AddHostedService<RefreshTokenCleanupService>();

            return services;
        }
    }
}
