using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for the tiered cancellation policy (#270): which tier applies at a given
    /// moment, that a blocked tier still throws the same error the single-threshold rule
    /// throws, and that a business without tiers keeps the old behaviour.
    /// </summary>
    public class CancellationPolicyTiersTests
    {
        private static readonly DateTime Start = new(2030, 1, 10, 12, 0, 0, DateTimeKind.Unspecified);

        // Free 24h ahead, 50% between 4h and 24h, blocked under 4h.
        private static CancellationPolicySnapshot Tiered(int? windowHours = null) => new(windowHours, new List<CancellationPolicyTier>
        {
            new() { MinHoursBefore = 24, PenaltyKind = CancellationPenaltyKind.None },
            new() { MinHoursBefore = 4, PenaltyKind = CancellationPenaltyKind.Percentage, PenaltyValue = 50m },
            new() { MinHoursBefore = 0, PenaltyKind = CancellationPenaltyKind.NotAllowed }
        });

        [Fact]
        public void WellAhead_AppliesTheFreeTier()
        {
            var applied = AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, Tiered(), Start.AddDays(-3));

            Assert.Equal(24, applied!.MinHoursBefore);
            Assert.Equal(CancellationPenaltyKind.None, applied.PenaltyKind);
        }

        [Fact]
        public void ExactlyOnAThreshold_StillGetsTheBetterTier()
        {
            // 24h ahead to the second: the client met the free threshold.
            var applied = AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, Tiered(), Start.AddHours(-24));

            Assert.Equal(24, applied!.MinHoursBefore);
        }

        [Fact]
        public void InsideTheMiddleTier_AppliesThePenalty()
        {
            var applied = AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, Tiered(), Start.AddHours(-12));

            Assert.Equal(4, applied!.MinHoursBefore);
            Assert.Equal(CancellationPenaltyKind.Percentage, applied.PenaltyKind);
            Assert.Equal(50m, applied.PenaltyValue);
        }

        [Fact]
        public void InsideTheBlockedTier_Throws_WithTheNoticeItWouldHaveNeeded()
        {
            var error = Assert.Throws<CancellationWindowElapsedException>(() =>
                AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, Tiered(), Start.AddHours(-1)));

            // The message quotes 4h: the last threshold that still allowed self-service.
            Assert.Contains("4h", error.Message);
            Assert.Equal("CANCELLATION_WINDOW_ELAPSED", error.Code);
        }

        [Fact]
        public void TiersWin_OverTheLegacyWindow()
        {
            // A 48h legacy window would block this, but the tiers are what the business
            // configured last, so 12h ahead is a penalised - not blocked - cancellation.
            var applied = AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, Tiered(windowHours: 48), Start.AddHours(-12));

            Assert.Equal(4, applied!.MinHoursBefore);
        }

        [Fact]
        public void WithoutTiers_TheSingleThresholdStillRules()
        {
            var policy = new CancellationPolicySnapshot(24, Array.Empty<CancellationPolicyTier>());

            Assert.Null(AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, policy, Start.AddDays(-2)));
            Assert.Throws<CancellationWindowElapsedException>(() =>
                AppointmentCancellationPolicy.EnsureSelfServiceAllowed(Start, policy, Start.AddHours(-1)));
        }
    }
}
