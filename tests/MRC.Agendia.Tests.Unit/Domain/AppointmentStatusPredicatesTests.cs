using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Domain
{
    /// <summary>
    /// Exhaustive truth table for the appointment-status predicates that gate the
    /// booking rules: which statuses may start a booking, and which are terminal.
    /// </summary>
    public class AppointmentStatusPredicatesTests
    {
        [Theory]
        [InlineData(AppointmentStatus.Pending, true)]
        [InlineData(AppointmentStatus.Confirmed, true)]
        [InlineData(AppointmentStatus.Cancelled, false)]
        [InlineData(AppointmentStatus.Completed, false)]
        [InlineData(AppointmentStatus.NoShow, false)]
        public void IsValidInitialStatus_matches_table(AppointmentStatus status, bool expected)
            => Assert.Equal(expected, status.IsValidInitialStatus());

        [Theory]
        [InlineData(AppointmentStatus.Pending, false)]
        [InlineData(AppointmentStatus.Confirmed, false)]
        [InlineData(AppointmentStatus.Cancelled, true)]
        [InlineData(AppointmentStatus.Completed, true)]
        [InlineData(AppointmentStatus.NoShow, true)]
        public void IsTerminal_matches_table(AppointmentStatus status, bool expected)
            => Assert.Equal(expected, status.IsTerminal());

        [Theory]
        [InlineData(AppointmentStatus.Pending, true)]
        [InlineData(AppointmentStatus.Confirmed, true)]
        [InlineData(AppointmentStatus.Cancelled, false)]
        [InlineData(AppointmentStatus.Completed, false)]
        [InlineData(AppointmentStatus.NoShow, false)]
        public void OccupiesCapacity_matches_table(AppointmentStatus status, bool expected)
            => Assert.Equal(expected, status.OccupiesCapacity());

        [Fact]
        public void Initial_statuses_are_never_terminal()
        {
            foreach (var status in Enum.GetValues<AppointmentStatus>())
                Assert.False(status.IsValidInitialStatus() && status.IsTerminal(),
                    $"{status} no puede ser inicial y terminal a la vez.");
        }
    }
}
