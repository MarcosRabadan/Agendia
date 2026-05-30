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
        /// <summary>Notifies the clients with an upcoming appointment in the current open slot that the business is running late.</summary>
        /// <param name="businessId">Business that is running late.</param>
        /// <param name="dto">Delay in minutes plus the scope (whole business or a single employee) and an optional cap on the number notified.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>How many clients were notified.</returns>
        Task<DelayNotificationResultDto> NotifyDelayAsync(int businessId, NotifyDelayDto dto, CancellationToken cancellationToken = default);
    }
}
