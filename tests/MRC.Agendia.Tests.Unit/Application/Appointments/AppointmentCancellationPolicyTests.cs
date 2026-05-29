using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for the pure self-service cancellation/reschedule window rule.
    /// </summary>
    public class AppointmentCancellationPolicyTests
    {
        private static readonly DateTime Start = new(2030, 1, 10, 12, 0, 0, DateTimeKind.Unspecified);

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-5)]
        public void EnsureSelfServiceAllowed_SinVentana_NoLanza(int? windowHours)
        {
            // No restriction even when "now" is right before the start.
            AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, windowHours, Start.AddMinutes(-1));
        }

        [Fact]
        public void EnsureSelfServiceAllowed_AntesDelLimite_NoLanza()
        {
            // 24h window: deadline is Start - 24h; now is well before it.
            AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, 24, Start.AddDays(-2));
        }

        [Fact]
        public void EnsureSelfServiceAllowed_JustoEnElLimite_NoLanza()
        {
            // now == deadline: still allowed (only strictly past the deadline is blocked).
            AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, 24, Start.AddHours(-24));
        }

        [Fact]
        public void EnsureSelfServiceAllowed_PasadoElLimite_Lanza()
        {
            var justAfterDeadline = Start.AddHours(-24).AddSeconds(1);
            Assert.Throws<CancellationWindowElapsedException>(() =>
                AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, 24, justAfterDeadline));
        }

        [Fact]
        public void EnsureSelfServiceAllowed_CitaInminente_Lanza()
        {
            // 1h before the start with a 24h window -> well past the deadline.
            Assert.Throws<CancellationWindowElapsedException>(() =>
                AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, 24, Start.AddHours(-1)));
        }
    }
}
