using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Behaviors;
using MRC.Agendia.Application.Business;
using MRC.Agendia.Application.Clients;
using MRC.Agendia.Application.Employees;
using MRC.Agendia.Application.Holidays;
using MRC.Agendia.Application.Mappings;
using MRC.Agendia.Application.Schedules;
using MRC.Agendia.Application.Services;
using MRC.Agendia.Application.Waitlist;

namespace MRC.Agendia.Application
{
    /// <summary>
    /// Single entry point to register every Application-layer service
    /// (MediatR, AutoMapper, application services).
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(CreateAppointmentCommand).Assembly;

            // MediatR: auto-discovers Commands/Queries/Handlers + the
            // ValidationBehavior that runs FluentValidation before any handler.
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // AutoMapper: auto-discovers profiles in the assembly.
            services.AddAutoMapper(typeof(BusinessProfile).Assembly);

            // FluentValidation: auto-discovers all AbstractValidator<T> in the assembly.
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            // Servicios de aplicacion (uno por agregado)
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IRecurringAppointmentService, RecurringAppointmentService>();
            services.AddScoped<IAppointmentDelayService, AppointmentDelayService>();
            services.AddScoped<IAppointmentSchedulingValidator, AppointmentSchedulingValidator>();
            services.AddScoped<IBusinessService, BusinessService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IServicesService, ServicesService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<IScheduleGenerationService, ScheduleGenerationService>();
            services.AddScoped<IHolidayService, HolidayService>();
            services.AddScoped<IAvailabilityService, AvailabilityService>();
            services.AddScoped<IWaitlistService, WaitlistService>();

            return services;
        }
    }
}
