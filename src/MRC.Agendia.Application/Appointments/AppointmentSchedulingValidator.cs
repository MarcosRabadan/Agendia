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
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmployeeTimeOffRepository _timeOffRepository;
        private readonly IWaitlistRepository _waitlistRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IClock _clock;

        public AppointmentSchedulingValidator(IBusinessRepository businessRepository,
                                              IEmployeeRepository employeeRepository,
                                              IServiceRepository serviceRepository,
                                              IAppointmentRepository appointmentRepository,
                                              IEmployeeTimeOffRepository timeOffRepository,
                                              IWaitlistRepository waitlistRepository,
                                              IScheduleResolver scheduleResolver,
                                              IClock clock)
        {
            _timeOffRepository = timeOffRepository;
            _waitlistRepository = waitlistRepository;
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _scheduleResolver = scheduleResolver;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task EnsureValidAsync(Guid? appointmentId,
                                           Guid employeeId,
                                           Guid serviceId,
                                           DateTime startDate,
                                           DateTime endDate,
                                           IReadOnlyCollection<Guid>? extraServiceIds = null,
                                           string? clientUserId = null,
                                           CancellationToken cancellationToken = default)
        {
            // ---------- Basic input checks ----------
            if (startDate == default || endDate == default)
                throw new InvalidAppointmentTimeException("StartDate and EndDate are required.");

            if (endDate <= startDate)
                throw new InvalidAppointmentTimeException("EndDate must be after StartDate.");

            if (startDate < _clock.BusinessNow)
                throw new InvalidAppointmentTimeException("Appointments cannot be created or moved to the past.");

            // ---------- Existence + activity ----------
            // The client is not validated here: it is a Harmony user id (the JWT sub),
            // already authenticated by the token; Agendia owns no client profile to check.
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
                    $"The appointment duration ({actualDuration:0.#} min) does not match the services' duration ({totalServiceMinutes} min).");
            }

            // ---------- Day must be open in the effective schedule ----------
            var date = DateOnly.FromDateTime(startDate);
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(employee.BusinessId, date, cancellationToken);

            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
            {
                throw new AppointmentOutsideScheduleException(
                    $"The business is closed on {date:yyyy-MM-dd}: {effective.ClosedReason ?? "no schedule"}.");
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
                    "The appointment is outside working hours or crosses a break between shifts.");
            }

            // ---------- Employee must not be blocked (#271) ----------
            // A time-off block takes the employee out entirely for its range, whatever
            // their capacity: a person who is away is away. Checked after the schedule so
            // "the business is closed" still wins as the more informative answer.
            if (await _timeOffRepository.HasOverlapAsync(employeeId, startDate, endDate, cancellationToken))
                throw new EmployeeUnavailableException();

            // ---------- Employee capacity check ----------
            // The employee can hold up to MaxConcurrentAppointments overlapping
            // appointments at the same time. Reject only when adding this one
            // would exceed that limit. Counted in the DB so we do not load the
            // whole day's appointments just to count one employee's.
            var overlappingCount = await _appointmentRepository.CountOverlappingForEmployeeAsync(
                employeeId, startDate, endDate, appointmentId, cancellationToken);

            // A waitlist priority hold (#268) reserves a seat for someone else, so it
            // counts as occupied here - and only for THEM is the slot still free. Reported
            // apart from a plain conflict: "wait a few minutes" is actionable, unlike
            // "it is full".
            var holds = await _waitlistRepository.GetActiveHoldsAsync(
                employee.BusinessId, DateOnly.FromDateTime(startDate), DateTime.UtcNow, cancellationToken);

            var othersHolds = holds.Count(h => h.StartTime == startTime
                && (h.EmployeeId is null || h.EmployeeId == employeeId)
                && h.ClientUserId != clientUserId);

            if (overlappingCount < employee.MaxConcurrentAppointments
                && overlappingCount + othersHolds >= employee.MaxConcurrentAppointments)
            {
                throw new SlotOnHoldException();
            }

            if (overlappingCount >= employee.MaxConcurrentAppointments)
            {
                throw new AppointmentConflictException(
                    employee.MaxConcurrentAppointments == 1
                        ? "The employee already has another appointment overlapping this time."
                        : $"The employee already has {overlappingCount} appointments at this time (max capacity: {employee.MaxConcurrentAppointments}).");
            }
        }
    }
}
