using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Business;
using MRC.Agendia.Application.Clients;
using MRC.Agendia.Application.Employees;
using MRC.Agendia.Application.Holidays;
using MRC.Agendia.Application.Mappings;
using MRC.Agendia.Application.Schedules;
using MRC.Agendia.Application.Services;

namespace MRC.Agendia.Application
{
    /// <summary>
    /// Punto de entrada unico para registrar todos los servicios de la capa Application
    /// (MediatR, AutoMapper, servicios de aplicacion).
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR: descubre Commands/Queries/Handlers en el assembly
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CreateAppointmentCommand).Assembly));

            // AutoMapper: descubre profiles en el assembly
            services.AddAutoMapper(typeof(BusinessProfile).Assembly);

            // Servicios de aplicacion (uno por agregado)
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IBusinessService, BusinessService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IServicesService, ServicesService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<IHolidayService, HolidayService>();

            return services;
        }
    }
}
