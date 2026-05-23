using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// The "is it in the past?" check must compare against the business wall-clock
    /// (<see cref="IClock.BusinessNow"/>), not UTC, so it stays coherent with the
    /// wall-clock appointment times (issue BIZ-01).
    /// </summary>
    public class AppointmentSchedulingValidatorTests
    {
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly AppointmentSchedulingValidator _sut;

        public AppointmentSchedulingValidatorTests()
        {
            _sut = new AppointmentSchedulingValidator(
                Substitute.For<IBusinessRepository>(),
                Substitute.For<IClientRepository>(),
                Substitute.For<IEmployeeRepository>(),
                Substitute.For<IServiceRepository>(),
                Substitute.For<IAppointmentRepository>(),
                Substitute.For<IScheduleResolver>(),
                _clock);
        }

        [Fact]
        public async Task EnsureValidAsync_StartAntesDeBusinessNow_Lanza()
        {
            _clock.BusinessNow.Returns(new DateTime(2026, 6, 1, 12, 0, 0));

            // One hour before "now" in the business timezone.
            var start = new DateTime(2026, 6, 1, 11, 0, 0);
            var end = new DateTime(2026, 6, 1, 11, 30, 0);

            await Assert.ThrowsAsync<InvalidAppointmentTimeException>(() =>
                _sut.EnsureValidAsync(null, clientId: 1, employeeId: 1, serviceId: 1, start, end));
        }
    }
}
