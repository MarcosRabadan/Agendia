using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentDelayService : IAppointmentDelayService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogger _auditLogger;
        private readonly IClock _clock;

        public AppointmentDelayService(IAppointmentRepository repository,
                                       IScheduleResolver scheduleResolver,
                                       IUnitOfWork unitOfWork,
                                       IAuditLogger auditLogger,
                                       IClock clock)
        {
            _repository = repository;
            _scheduleResolver = scheduleResolver;
            _unitOfWork = unitOfWork;
            _auditLogger = auditLogger;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<DelayNotificationResultDto> NotifyDelayAsync(Guid businessId, NotifyDelayDto dto, CancellationToken cancellationToken = default)
        {
            var now = _clock.BusinessNow;
            var today = DateOnly.FromDateTime(now);

            // The affected slot is the current open slot (or the next one if we are
            // before it / in a break). Picking a single slot is what keeps a morning
            // delay from reaching the afternoon shift across the split-shift break.
            var schedule = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, today, cancellationToken);
            var nowTime = TimeOnly.FromDateTime(now);
            var slot = schedule.IsOpen
                ? schedule.TimeSlots.Where(s => s.EndTime > nowTime).OrderBy(s => s.StartTime).FirstOrDefault()
                : null;
            if (slot is null)
                return new DelayNotificationResultDto(0);

            // Today only and after "now"; the repository already excludes soft-deleted
            // participants and inactive employees (BIZ-03) and orders by StartDate.
            var dayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var candidates = await _repository.GetUpcomingForDelayAsync(businessId, dto.EmployeeId, now, dayEnd, cancellationToken);

            var affected = candidates
                .Where(a =>
                {
                    var start = TimeOnly.FromDateTime(a.StartDate);
                    return start >= slot.StartTime && start < slot.EndTime;
                })
                .ToList();

            if (dto.MaxAppointments is int max)
                affected = affected.Take(max).ToList();

            if (affected.Count == 0)
                return new DelayNotificationResultDto(0);

            // Business id and language are properties of the BUSINESS, and the repository already
            // filtered every affected appointment by this one, so they are resolved ONCE instead
            // of once per appointment. Taken from the first one's employee: with a null
            // dto.EmployeeId the list spans several employees, but they all hang off this
            // business, so any of them answers the same (#295).
            var business = await _repository.GetNotificationBusinessByEmployeeAsync(
                affected[0].EmployeeId, cancellationToken);
            if (business is null)
                return new DelayNotificationResultDto(0);

            // Raise a delay event on each affected (tracked) appointment; the DbContext
            // SaveChanges override enlists them all into the outbox in the single Save below
            // (the consumer resolves each client's contact from ClientUserId and delivers in
            // the business language). Everything about the appointment comes off the entity
            // (see IAppointmentRepository.GetNotificationBusinessByEmployeeAsync, #293).
            foreach (var appointment in affected)
            {
                appointment.RaiseEvent(new AppointmentDelayed(
                    appointment.Id, business.BusinessId, appointment.EmployeeId, appointment.ClientUserId,
                    appointment.ServiceId, appointment.StartDate, appointment.EndDate,
                    dto.DelayMinutes, business.Language, _clock.TimeZoneId, DateTime.UtcNow));
            }

            await _unitOfWork.Save(cancellationToken);

            await _auditLogger.LogAsync(
                AuditActions.AppointmentDelayNotified, "Business", businessId.ToString(),
                new { dto.EmployeeId, dto.DelayMinutes, notified = affected.Count }, cancellationToken);

            return new DelayNotificationResultDto(affected.Count);
        }
    }
}
