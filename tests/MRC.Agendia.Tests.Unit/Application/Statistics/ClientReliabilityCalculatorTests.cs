using MRC.Agendia.Application.Statistics;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Statistics
{
    /// <summary>
    /// Unit tests for the pure reliability aggregation: the no-show rate is measured
    /// against the appointments that were meant to happen (completed + no-show), while
    /// cancellations get their own rate over the total.
    /// </summary>
    public class ClientReliabilityCalculatorTests
    {
        private static readonly Guid BusinessId = TestIds.Of(1);
        private const string ClientUserId = "harmony-client-1";
        private static readonly DateOnly From = new(2026, 5, 1);
        private static readonly DateOnly To = new(2026, 7, 30);

        private static ClientReliabilityDto Calculate(params AppointmentStatus[] statuses) =>
            ClientReliabilityCalculator.Calculate(statuses, BusinessId, ClientUserId, From, To);

        [Fact]
        public void NoAppointments_IsAllZeros_AndRatesDoNotDivideByZero()
        {
            var result = Calculate();

            Assert.Equal(0, result.Total);
            Assert.Equal(0, result.NoShowRate);
            Assert.Equal(0, result.CancellationRate);
            Assert.Equal(ClientUserId, result.ClientUserId);
            Assert.Equal(BusinessId, result.BusinessId);
            Assert.Equal(From, result.From);
            Assert.Equal(To, result.To);
        }

        [Fact]
        public void AllCompleted_IsAPerfectRecord()
        {
            var result = Calculate(AppointmentStatus.Completed, AppointmentStatus.Completed);

            Assert.Equal(2, result.Total);
            Assert.Equal(2, result.Completed);
            Assert.Equal(0, result.NoShowRate);
            Assert.Equal(0, result.CancellationRate);
        }

        [Fact]
        public void MixedOutcomes_RateCountersAreSplitAsDocumented()
        {
            // 2 completed, 1 no-show, 1 cancelled, 1 still open (never closed by the staff).
            var result = Calculate(
                AppointmentStatus.Completed,
                AppointmentStatus.Completed,
                AppointmentStatus.NoShow,
                AppointmentStatus.Cancelled,
                AppointmentStatus.Confirmed);

            Assert.Equal(5, result.Total);
            Assert.Equal(2, result.Completed);
            Assert.Equal(1, result.NoShow);
            Assert.Equal(1, result.Cancelled);

            // No-show over completed + no-show (3), NOT over the total.
            Assert.Equal(Math.Round(1d / 3, 4), result.NoShowRate);
            // Cancellations over the whole total (5).
            Assert.Equal(0.2, result.CancellationRate);
        }

        [Fact]
        public void OnlyCancellations_LeaveTheNoShowRateAtZero()
        {
            var result = Calculate(AppointmentStatus.Cancelled, AppointmentStatus.Cancelled);

            Assert.Equal(0, result.NoShowRate);
            Assert.Equal(1, result.CancellationRate);
        }
    }
}
