using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Notifies the clients of upcoming appointments that the business is running
    /// late. Only the appointments in the current continuous time slot are affected
    /// (a morning delay does not propagate across a split-shift break), and only
    /// those after "now".
    /// </summary>
    public interface IAppointmentDelayService
    {
        Task<DelayNotificationResultDto> NotifyDelayAsync(int businessId, NotifyDelayDto dto, CancellationToken cancellationToken = default);
    }
}
