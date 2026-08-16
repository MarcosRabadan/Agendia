using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Availability
{
    public class AvailabilityService : IAvailabilityService
    {
        private const int MinStepMinutes = 5;
        private const int MaxStepMinutes = 120;

        private readonly IBusinessRepository _businessRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IClock _clock;

        public AvailabilityService(IBusinessRepository businessRepository,
                                   IServiceRepository serviceRepository,
                                   IEmployeeRepository employeeRepository,
                                   IAppointmentRepository appointmentRepository,
                                   IScheduleResolver scheduleResolver,
                                   IClock clock)
        {
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _appointmentRepository = appointmentRepository;
            _scheduleResolver = scheduleResolver;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<AvailabilityDto> GetAvailabilityAsync(Guid businessId,
                                                                DateOnly date,
                                                                Guid serviceId,
                                                                Guid? employeeId,
                                                                int stepMinutes = 15,
                                                                IReadOnlyCollection<Guid>? extraServiceIds = null,
                                                                CancellationToken cancellationToken = default)
        {
            // ---------- Validate inputs ----------
            if (stepMinutes < MinStepMinutes || stepMinutes > MaxStepMinutes)
                throw new ArgumentException(
                    $"stepMinutes must be between {MinStepMinutes} and {MaxStepMinutes}.");

            // Public booking read: resolve the business unscoped (GetActiveByIdAsync
            // uses IgnoreQueryFilters) so an authenticated owner/employee browsing
            // ANOTHER business still gets it (#58). An inactive or soft-deleted
            // business is not bookable -> 404, like the public business-detail read.
            _ = await _businessRepository.GetActiveByIdAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException(businessId);

            var service = await _serviceRepository.GetByIdPublicAsync(serviceId, cancellationToken)
                ?? throw new ServiceNotFoundException(serviceId);

            if (service.BusinessId != businessId)
                throw new InvalidOperationException(
                    "The service does not belong to the given business.");

            if (service.DurationMinutes <= 0)
                throw new InvalidOperationException(
                    "The service has no valid duration.");

            // Multiservice (#170): the slot must fit the SUM of the primary service
            // plus any extra services; each extra must belong to the same business.
            var totalDurationMinutes = service.DurationMinutes;
            if (extraServiceIds is { Count: > 0 })
            {
                foreach (var extraId in extraServiceIds)
                {
                    var extra = await _serviceRepository.GetByIdPublicAsync(extraId, cancellationToken)
                        ?? throw new ServiceNotFoundException(extraId);
                    if (extra.BusinessId != businessId)
                        throw new InvalidOperationException("An extra service does not belong to the given business.");
                    if (extra.DurationMinutes <= 0)
                        throw new InvalidOperationException("An extra service has no valid duration.");
                    totalDurationMinutes += extra.DurationMinutes;
                }
            }

            // ---------- Pick the candidate employees ----------
            List<Employee> employees;
            if (employeeId is Guid empId)
            {
                var employee = await _employeeRepository.GetByIdPublicAsync(empId, cancellationToken)
                    ?? throw new EmployeeNotFoundException(empId);

                if (employee.BusinessId != businessId)
                    throw new InvalidOperationException(
                        "The employee does not belong to the given business.");

                if (!employee.IsActive)
                    return EmptyAvailability(
                        date, businessId, serviceId, employeeId,
                        totalDurationMinutes, stepMinutes,
                        "The selected employee is inactive.");

                employees = new List<Employee> { employee };
            }
            else
            {
                employees = (await _employeeRepository
                    .GetActiveByBusinessIdAsync(businessId, cancellationToken))
                    .ToList();
            }

            if (employees.Count == 0)
            {
                return EmptyAvailability(
                    date, businessId, serviceId, employeeId,
                    totalDurationMinutes, stepMinutes,
                    "No active employees in this business.");
            }

            // ---------- Resolve the day's effective schedule ----------
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date, cancellationToken);
            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
            {
                return EmptyAvailability(
                    date, businessId, serviceId, employeeId,
                    totalDurationMinutes, stepMinutes,
                    effective.ClosedReason ?? "Closed.");
            }

            // ---------- Load existing appointments for that day ----------
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));

            var appointments = (await _appointmentRepository
                    .GetByBusinessIdAndDateRangeAsync(businessId, dayStart, dayEnd, cancellationToken))
                .Where(a => a.Status.OccupiesCapacity())
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ---------- Compute slots ----------
            // For each candidate window, per employee:
            //   remainingCapacity = MaxConcurrentAppointments - overlappingCount
            // If at least one employee has remainingCapacity > 0 the slot is
            // bookable; total Capacity is the sum across all employees.
            var duration = TimeSpan.FromMinutes(totalDurationMinutes);
            var step = TimeSpan.FromMinutes(stepMinutes);
            var slots = new List<AvailableSlotDto>();

            // Wall-clock "now" in the business timezone: slots that already started
            // are not bookable (the scheduling validator rejects past times).
            var nowLocal = _clock.BusinessNow;

            foreach (var window in effective.TimeSlots.OrderBy(ts => ts.StartTime))
            {
                var current = window.StartTime;

                while (current.Add(duration) <= window.EndTime)
                {
                    var slotEnd = current.Add(duration);

                    if (date.ToDateTime(current) < nowLocal)
                    {
                        current = current.Add(step);
                        continue;
                    }

                    var availableEmployeeIds = new List<Guid>();
                    var totalCapacity = 0;

                    foreach (var employee in employees)
                    {
                        var overlapping = CountOverlapping(appointments, employee.Id, date, current, slotEnd);
                        var remaining = employee.MaxConcurrentAppointments - overlapping;
                        if (remaining > 0)
                        {
                            availableEmployeeIds.Add(employee.Id);
                            totalCapacity += remaining;
                        }
                    }

                    if (totalCapacity > 0)
                    {
                        slots.Add(new AvailableSlotDto(
                            StartTime: current,
                            EndTime: slotEnd,
                            Capacity: totalCapacity,
                            AvailableEmployeeIds: availableEmployeeIds));
                    }

                    current = current.Add(step);
                }
            }

            return new AvailabilityDto(
                Date: date,
                BusinessId: businessId,
                ServiceId: serviceId,
                EmployeeId: employeeId,
                DurationMinutes: totalDurationMinutes,
                StepMinutes: stepMinutes,
                IsOpen: true,
                ClosedReason: null,
                Slots: slots);
        }

        /// <inheritdoc />
        public async Task<int?> GetSlotCapacityAsync(Guid businessId,
                                                     DateOnly date,
                                                     TimeOnly startTime,
                                                     Guid serviceId,
                                                     Guid? employeeId,
                                                     CancellationToken cancellationToken = default)
        {
            _ = await _businessRepository.GetActiveByIdAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException(businessId);

            var service = await _serviceRepository.GetByIdPublicAsync(serviceId, cancellationToken)
                ?? throw new ServiceNotFoundException(serviceId);
            if (service.BusinessId != businessId)
                throw new InvalidOperationException("The service does not belong to the given business.");
            if (service.DurationMinutes <= 0)
                throw new InvalidOperationException("The service has no valid duration.");

            List<Employee> employees;
            if (employeeId is Guid empId)
            {
                var employee = await _employeeRepository.GetByIdPublicAsync(empId, cancellationToken)
                    ?? throw new EmployeeNotFoundException(empId);
                if (employee.BusinessId != businessId)
                    throw new InvalidOperationException("The employee does not belong to the given business.");
                if (!employee.IsActive)
                    return null;
                employees = new List<Employee> { employee };
            }
            else
            {
                employees = (await _employeeRepository.GetActiveByBusinessIdAsync(businessId, cancellationToken)).ToList();
            }

            if (employees.Count == 0)
                return null;

            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date, cancellationToken);
            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
                return null;

            var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
            // The slot must fit entirely inside one continuous open window.
            if (!effective.TimeSlots.Any(w => startTime >= w.StartTime && endTime <= w.EndTime))
                return null;

            var start = date.ToDateTime(startTime);
            var end = date.ToDateTime(endTime);
            var capacity = 0;
            foreach (var employee in employees)
            {
                var overlapping = await _appointmentRepository.CountOverlappingForEmployeeAsync(employee.Id, start, end, null, cancellationToken);
                var remaining = employee.MaxConcurrentAppointments - overlapping;
                if (remaining > 0)
                    capacity += remaining;
            }

            return capacity;
        }

        /// <summary>
        /// Counts how many of the employee's existing appointments overlap with
        /// the candidate window [slotStart, slotEnd] on the given date.
        /// </summary>
        private static int CountOverlapping(Dictionary<Guid, List<Appointment>> appointmentsByEmployee,
                                            Guid employeeId,
                                            DateOnly date,
                                            TimeOnly slotStart,
                                            TimeOnly slotEnd)
        {
            if (!appointmentsByEmployee.TryGetValue(employeeId, out var appts) || appts.Count == 0)
                return 0;

            var slotStartDt = date.ToDateTime(slotStart);
            var slotEndDt = date.ToDateTime(slotEnd);

            // Two ranges [a.Start, a.End) and [slotStart, slotEnd) overlap iff
            // a.Start < slotEnd && a.End > slotStart.
            return appts.Count(a => a.StartDate < slotEndDt && a.EndDate > slotStartDt);
        }

        private static AvailabilityDto EmptyAvailability(
            DateOnly date, Guid businessId, Guid serviceId, Guid? employeeId,
            int durationMinutes, int stepMinutes, string closedReason)
        {
            return new AvailabilityDto(
                Date: date,
                BusinessId: businessId,
                ServiceId: serviceId,
                EmployeeId: employeeId,
                DurationMinutes: durationMinutes,
                StepMinutes: stepMinutes,
                IsOpen: false,
                ClosedReason: closedReason,
                Slots: Array.Empty<AvailableSlotDto>());
        }
    }
}
