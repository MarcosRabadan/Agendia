using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Domain
{
    public class AppointmentStatusExtensionsTests
    {
        [Theory]
        [InlineData(AppointmentStatus.Pending, true)]
        [InlineData(AppointmentStatus.Confirmed, true)]
        [InlineData(AppointmentStatus.Cancelled, false)]
        [InlineData(AppointmentStatus.Completed, false)]
        [InlineData(AppointmentStatus.NoShow, false)]
        public void OccupiesCapacity_SoloPendingYConfirmed(AppointmentStatus status, bool expected)
        {
            Assert.Equal(expected, status.OccupiesCapacity());
        }
    }
}
