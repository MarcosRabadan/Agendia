using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentSchedulingValidator : IAppointmentSchedulingValidator
    {
        // Small tolerance for rounding when comparing minute counts.
        private const double DurationToleranceMinutes = 0.5;

        private readonly IBusinessRepository _businessRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IClock _clock;

        public AppointmentSchedulingValidator(
            IBusinessRepository businessRepository,
            IClientRepository clientRepository,
            IEmployeeRepository employeeRepository,
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IScheduleResolver scheduleResolver,
            IClock clock)
        {
            _businessRepository = businessRepository;
            _clientRepository = clientRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _scheduleResolver = scheduleResolver;
            _clock = clock;
        }

        public async Task EnsureValidAsync(
            int? appointmentId,
            int clientId,
            int employeeId,
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            IReadOnlyCollection<int>? extraServiceIds = null,
            CancellationToken cancellationToken = default)
        {
            // ---------- Basic input checks ----------
            if (startDate == default || endDate == default)
                throw new InvalidAppointmentTimeException("StartDate y EndDate son obligatorios.");

            if (endDate <= startDate)
                throw new InvalidAppointmentTimeException("EndDate debe ser posterior a StartDate.");

            if (startDate < _clock.BusinessNow)
                throw new InvalidAppointmentTimeException("No se pueden crear ni mover citas al pasado.");

            // ---------- Existence + activity ----------
            _ = await _clientRepository.GetByIdAsync(clientId, cancellationToken)
                ?? throw new ClientNotFoundException(clientId);

            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
                ?? throw new EmployeeNotFoundException(employeeId);
            if (!employee.IsActive)
                throw new EmployeeInactiveException();

            // A soft-deleted business is filtered out here, so its still-alive
            // employees/services cannot be booked (consistent with AvailabilityService).
            _ = await _businessRepository.GetByIdAsync(employee.BusinessId, cancellationToken)
                ?? throw new BusinessNotFoundException(employee.BusinessId);

            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken)
                ?? throw new ServiceNotFoundException(serviceId);

            if (service.BusinessId != employee.BusinessId)
                throw new ServiceEmployeeMismatchException();

            // ---------- Total duration matches the sum of all services (#170) ----------
            // The appointment may bundle extra services in the same visit (cut +
            // beard); its block must span the primary service plus every extra, and
            // each extra must belong to the same business.
            var totalServiceMinutes = service.DurationMinutes;
            if (extraServiceIds is { Count: > 0 })
            {
                foreach (var extraId in extraServiceIds)
                {
                    var extra = await _serviceRepository.GetByIdAsync(extraId, cancellationToken)
                        ?? throw new ServiceNotFoundException(extraId);
                    if (extra.BusinessId != employee.BusinessId)
                        throw new ServiceEmployeeMismatchException();
                    totalServiceMinutes += extra.DurationMinutes;
                }
            }

            var actualDuration = (endDate - startDate).TotalMinutes;
            if (Math.Abs(actualDuration - totalServiceMinutes) > DurationToleranceMinutes)
            {
                throw new AppointmentDurationMismatchException(
                    $"La duracion de la cita ({actualDuration:0.#} min) no coincide con la de los servicios ({totalServiceMinutes} min).");
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
            // would exceed that limit. Counted in the DB so we do not load the
            // whole day's appointments just to count one employee's.
            var overlappingCount = await _appointmentRepository.CountOverlappingForEmployeeAsync(
                employeeId, startDate, endDate, appointmentId, cancellationToken);

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
