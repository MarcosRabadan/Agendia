using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
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
            _ = await _clientRepository.GetByIdAsync(clientId, cancellationToken)
                ?? throw new ClientNotFoundException(clientId);

            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
                ?? throw new EmployeeNotFoundException(employeeId);
            if (!employee.IsActive)
                throw new InvalidOperationException("El empleado indicado esta inactivo.");

            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken)
                ?? throw new ServiceNotFoundException(serviceId);

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
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(employee.BusinessId, date, cancellationToken);

            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
            {
                throw new AppointmentOutsideScheduleException(
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
                throw new AppointmentOutsideScheduleException(
                    "La cita esta fuera del horario laboral o cruza un descanso entre turnos.");
            }

            // ---------- Employee capacity check ----------
            // The employee can hold up to MaxConcurrentAppointments overlapping
            // appointments at the same time. Reject only when adding this one
            // would exceed that limit.
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));

            var overlappingCount = (await _appointmentRepository
                    .GetByBusinessIdAndDateRangeAsync(employee.BusinessId, dayStart, dayEnd, cancellationToken))
                .Where(a => a.EmployeeId == employeeId)
                .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
                .Where(a => appointmentId is null || a.Id != appointmentId.Value)
                .Count(a => a.StartDate < endDate && a.EndDate > startDate);

            if (overlappingCount >= employee.MaxConcurrentAppointments)
            {
                throw new AppointmentConflictException(
                    employee.MaxConcurrentAppointments == 1
                        ? "El empleado ya tiene otra cita que se solapa con este horario."
                        : $"El empleado ya tiene {overlappingCount} citas en este horario (capacidad maxima: {employee.MaxConcurrentAppointments}).");
            }
        }
    }
}
