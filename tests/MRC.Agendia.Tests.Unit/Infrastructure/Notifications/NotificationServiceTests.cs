using Microsoft.Extensions.Logging;
using MRC.Agendia.Infrastructure.Notifications;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Notifications
{
    /// <summary>
    /// The Client entity was removed, so Agendia can no longer resolve a recipient and
    /// <see cref="NotificationService"/> is a temporary no-op until delivery moves to
    /// events (#246). Every method must report the notification as handled (return
    /// true) so the reminder job and the waitlist trigger do not retry it forever.
    /// </summary>
    public class NotificationServiceTests
    {
        private readonly NotificationService _sut =
            new(Substitute.For<ILogger<NotificationService>>());

        [Fact]
        public async Task Confirmation_EsNoOp_DevuelveTrue()
            => Assert.True(await _sut.SendAppointmentConfirmationAsync(5));

        [Fact]
        public async Task Reminder_EsNoOp_DevuelveTrue()
            => Assert.True(await _sut.SendAppointmentReminderAsync(5));

        [Fact]
        public async Task Cancellation_EsNoOp_DevuelveTrue()
            => Assert.True(await _sut.SendAppointmentCancellationAsync(5));

        [Fact]
        public async Task Delay_EsNoOp_DevuelveTrue()
            => Assert.True(await _sut.SendDelayNotificationAsync(5, 15));

        [Fact]
        public async Task Waitlist_EsNoOp_DevuelveTrue()
            => Assert.True(await _sut.SendWaitlistAvailabilityAsync(7));
    }
}
