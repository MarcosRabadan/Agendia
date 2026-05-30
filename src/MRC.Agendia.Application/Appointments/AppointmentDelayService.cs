using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentDelayService : IAppointmentDelayService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogger _auditLogger;
        private readonly IClock _clock;

        public AppointmentDelayService(
            IAppointmentRepository repository,
            IScheduleResolver scheduleResolver,
            INotificationService notificationService,
            IAuditLogger auditLogger,
            IClock clock)
        {
            _repository = repository;
            _scheduleResolver = scheduleResolver;
            _notificationService = notificationService;
            _auditLogger = auditLogger;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<DelayNotificationResultDto> NotifyDelayAsync(int businessId, NotifyDelayDto dto, CancellationToken cancellationToken = default)
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

            // Best-effort per-client notification (never throws out of the service).
            foreach (var appointment in affected)
                await _notificationService.SendDelayNotificationAsync(appointment.Id, dto.DelayMinutes, cancellationToken);

            if (affected.Count > 0)
            {
                await _auditLogger.LogAsync(
                    AuditActions.AppointmentDelayNotified, "Business", businessId.ToString(),
                    new { dto.EmployeeId, dto.DelayMinutes, notified = affected.Count }, cancellationToken);
            }

            return new DelayNotificationResultDto(affected.Count);
        }
    }
}
