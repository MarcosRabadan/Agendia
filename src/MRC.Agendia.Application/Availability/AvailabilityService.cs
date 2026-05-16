using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Availability
{
    public class AvailabilityService : IAvailabilityService
    {
        private const int MinStepMinutes = 5;
        private const int MaxStepMinutes = 120;

        private readonly IServiceRepository _serviceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IScheduleResolver _scheduleResolver;

        public AvailabilityService(
            IServiceRepository serviceRepository,
            IEmployeeRepository employeeRepository,
            IAppointmentRepository appointmentRepository,
            IScheduleResolver scheduleResolver)
        {
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _appointmentRepository = appointmentRepository;
            _scheduleResolver = scheduleResolver;
        }

        public async Task<AvailabilityDto> GetAvailabilityAsync(
            int businessId,
            DateOnly date,
            int serviceId,
            int? employeeId,
            int stepMinutes = 15)
        {
            // ---------- Validate inputs ----------
            if (stepMinutes < MinStepMinutes || stepMinutes > MaxStepMinutes)
                throw new ArgumentException(
                    $"stepMinutes debe estar entre {MinStepMinutes} y {MaxStepMinutes}.");

            var service = await _serviceRepository.GetByIdAsync(serviceId)
                ?? throw new KeyNotFoundException($"Service {serviceId} not found.");

            if (service.BusinessId != businessId)
                throw new InvalidOperationException(
                    "El servicio no pertenece al negocio indicado.");

            if (service.DurationMinutes <= 0)
                throw new InvalidOperationException(
                    "El servicio no tiene una duracion valida.");

            // ---------- Pick the candidate employees ----------
            List<Employee> employees;
            if (employeeId is int empId)
            {
                var employee = await _employeeRepository.GetByIdAsync(empId)
                    ?? throw new KeyNotFoundException($"Employee {empId} not found.");

                if (employee.BusinessId != businessId)
                    throw new InvalidOperationException(
                        "El empleado no pertenece al negocio indicado.");

                if (!employee.IsActive)
                    return EmptyAvailability(
                        date, businessId, serviceId, employeeId,
                        service.DurationMinutes, stepMinutes,
                        "El empleado indicado esta inactivo.");

                employees = new List<Employee> { employee };
            }
            else
            {
                employees = (await _employeeRepository
                    .GetByBusinessIdAsync(businessId, onlyActive: true))
                    .ToList();
            }

            if (employees.Count == 0)
            {
                return EmptyAvailability(
                    date, businessId, serviceId, employeeId,
                    service.DurationMinutes, stepMinutes,
                    "No hay empleados activos en este negocio.");
            }

            // ---------- Resolve the day's effective schedule ----------
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date);
            if (!effective.IsOpen || effective.TimeSlots.Count == 0)
            {
                return EmptyAvailability(
                    date, businessId, serviceId, employeeId,
                    service.DurationMinutes, stepMinutes,
                    effective.ClosedReason ?? "Cerrado.");
            }

            // ---------- Load existing appointments for that day ----------
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.ToDateTime(new TimeOnly(23, 59, 59));

            var appointments = (await _appointmentRepository
                    .GetByBusinessIdAndDateRangeAsync(businessId, dayStart, dayEnd))
                .Where(a => a.Status == AppointmentStatus.Pending
                         || a.Status == AppointmentStatus.Confirmed)
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ---------- Compute slots ----------
            var duration = TimeSpan.FromMinutes(service.DurationMinutes);
            var step = TimeSpan.FromMinutes(stepMinutes);
            var slots = new List<AvailableSlotDto>();

            foreach (var window in effective.TimeSlots.OrderBy(ts => ts.StartTime))
            {
                var current = window.StartTime;

                while (current.Add(duration) <= window.EndTime)
                {
                    var slotEnd = current.Add(duration);
                    var availableEmployeeIds = new List<int>();

                    foreach (var employee in employees)
                    {
                        if (!HasConflict(appointments, employee.Id, date, current, slotEnd))
                            availableEmployeeIds.Add(employee.Id);
                    }

                    if (availableEmployeeIds.Count > 0)
                    {
                        slots.Add(new AvailableSlotDto(current, slotEnd, availableEmployeeIds));
                    }

                    current = current.Add(step);
                }
            }

            return new AvailabilityDto(
                Date: date,
                BusinessId: businessId,
                ServiceId: serviceId,
                EmployeeId: employeeId,
                DurationMinutes: service.DurationMinutes,
                StepMinutes: stepMinutes,
                IsOpen: true,
                ClosedReason: null,
                Slots: slots);
        }

        /// <summary>
        /// True if the candidate window [slotStart, slotEnd] on the given date
        /// overlaps with any of the employee's existing appointments.
        /// </summary>
        private static bool HasConflict(
            Dictionary<int, List<Appointment>> appointmentsByEmployee,
            int employeeId,
            DateOnly date,
            TimeOnly slotStart,
            TimeOnly slotEnd)
        {
            if (!appointmentsByEmployee.TryGetValue(employeeId, out var appts) || appts.Count == 0)
                return false;

            var slotStartDt = date.ToDateTime(slotStart);
            var slotEndDt = date.ToDateTime(slotEnd);

            // Two ranges [a.Start, a.End) and [slotStart, slotEnd) overlap iff
            // a.Start < slotEnd && a.End > slotStart.
            return appts.Any(a => a.StartDate < slotEndDt && a.EndDate > slotStartDt);
        }

        private static AvailabilityDto EmptyAvailability(
            DateOnly date, int businessId, int serviceId, int? employeeId,
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
