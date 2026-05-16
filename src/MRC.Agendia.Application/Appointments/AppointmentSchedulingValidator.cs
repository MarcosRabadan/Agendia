using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentSchedulingValidator : IAppointmentSchedulingValidator
    {
        // Small tolerance for rounding when comparing minute counts.
        private const double DurationToleranceMinutes = 0.5;

        private readonly IClientRepository _clientRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IScheduleResolver _scheduleResolver;

        public AppointmentSchedulingValidator(
            IClientRepository clientRepository,
            IEmployeeRepository employeeRepository,
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IScheduleResolver scheduleResolver)
        {
            _clientRepository = clientRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _scheduleResolver = scheduleResolver;
        }

        public async Task EnsureValidAsync(
            int? appointmentId,
            int clientId,
            int employeeId,
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            // ---------- Basic input checks ----------
            if (startDate == default || endDate == default)
                throw new InvalidOperationException("StartDate y EndDate son obligatorios.");

            if (endDate <= startDate)
                throw new InvalidOperationException("EndDate debe ser posterior a StartDate.");

            if (startDate < DateTime.UtcNow)
                throw new InvalidOperationException("No se pueden crear ni mover citas al pasado.");

            // ---------- Existence + activity ----------
            var client = await _clientRepository.GetByIdAsync(clientId)
                ?? throw new KeyNotFoundException($"Client {clientId} not found.");

            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException($"Employee {employeeId} not found.");
            if (!employee.IsActive)
                throw new InvalidOperationException("El empleado indicado esta inactivo.");

            var service = await _serviceRepository.GetByIdAsync(serviceId)
                ?? throw new KeyNotFoundException($"Service {serviceId} not found.");

            if (service.BusinessId != employee.BusinessId)
                throw new InvalidOperationException(
                    "El servicio y el empleado pertenecen a negocios distintos.");

            // ---------- Duration matches the service ----------
            var actualDuration = (endDate - startDate).TotalMinutes;
            if (Math.Abs(actualDuration - service.DurationMinutes) > DurationToleranceMinutes)
            {
                throw new InvalidOperationException(
                    $"La duracion de la cita ({actualDuration:0.#} min) no coincide con la del servicio ({service.DurationMinutes} min).");
            }

            // ---------- Day must be open in the effective schedule ----------
            var date = DateOnly.FromDateTime(startDate);
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(employee.BusinessId, date);

            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
            {
                throw new InvalidOperationException(
                    $"El negocio esta cerrado el {date:yyyy-MM-dd}: {effective.ClosedReason ?? "sin horario"}.");
            }

            // ---------- Appointment must fit inside ONE continuous open slot ----------
            // (otherwise it would span the break between morning and afternoon shifts).
            var startTime = TimeOnly.FromDateTime(startDate);
            var endTime = TimeOnly.FromDateTime(endDate);

            var fitsInsideSomeSlot = effective.TimeSlots.Any(ts =>
                startTime >= ts.StartTime && endTime <= ts.EndTime);

            if (!fitsInsideSomeSlot)
            {
                throw new InvalidOperationException(
                    "La cita esta fuera del horario laboral o cruza un descanso entre turnos.");
            }

            // ---------- No overlap with other appointments of the same employee ----------
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));

            var conflicts = (await _appointmentRepository
                    .GetByBusinessIdAndDateRangeAsync(employee.BusinessId, dayStart, dayEnd))
                .Where(a => a.EmployeeId == employeeId)
                .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
                .Where(a => appointmentId is null || a.Id != appointmentId.Value)
                .Any(a => a.StartDate < endDate && a.EndDate > startDate);

            if (conflicts)
            {
                throw new InvalidOperationException(
                    "El empleado ya tiene otra cita que se solapa con este horario.");
            }
        }
    }
}
