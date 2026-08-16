using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// The "is it in the past?" check must compare against the business wall-clock
    /// (<see cref="IClock.BusinessNow"/>), not UTC (issue BIZ-01), and the duration
    /// check must use the sum of the primary service plus any extras (#170).
    /// </summary>
    public class AppointmentSchedulingValidatorTests
    {
        private static readonly Guid BusinessId = TestIds.Of(1);
        private static readonly Guid EmployeeId = TestIds.Of(10);
        private static readonly Guid PrimaryServiceId = TestIds.Of(100);
        private static readonly Guid ExtraServiceId = TestIds.Of(200);

        private readonly IBusinessRepository _businessRepository = Substitute.For<IBusinessRepository>();
        private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
        private readonly IServiceRepository _serviceRepository = Substitute.For<IServiceRepository>();
        private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
        private readonly IEmployeeTimeOffRepository _timeOffRepository = Substitute.For<IEmployeeTimeOffRepository>();
        private readonly IWaitlistRepository _waitlistRepository = Substitute.For<IWaitlistRepository>();
        private readonly IScheduleResolver _scheduleResolver = Substitute.For<IScheduleResolver>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly AppointmentSchedulingValidator _sut;

        public AppointmentSchedulingValidatorTests()
        {
            _sut = new AppointmentSchedulingValidator(
                _businessRepository, _employeeRepository,
                _serviceRepository, _appointmentRepository, _timeOffRepository, _waitlistRepository, _scheduleResolver, _clock);
        }

        [Fact]
        public async Task EnsureValidAsync_StartAntesDeBusinessNow_Lanza()
        {
            _clock.BusinessNow.Returns(new DateTime(2026, 6, 1, 12, 0, 0));

            // One hour before "now" in the business timezone.
            var start = new DateTime(2026, 6, 1, 11, 0, 0);
            var end = new DateTime(2026, 6, 1, 11, 30, 0);

            await Assert.ThrowsAsync<InvalidAppointmentTimeException>(() =>
                _sut.EnsureValidAsync(null, employeeId: TestIds.Of(1), serviceId: TestIds.Of(1), start, end));
        }

        [Fact]
        public async Task EnsureValidAsync_Multiservicio_DuracionTotalCorrecta_NoLanza()
        {
            ArrangeOpenDayWithServices(primaryMinutes: 30, extraMinutes: 45);
            var start = new DateTime(2030, 6, 3, 9, 0, 0);
            var end = start.AddMinutes(75); // 30 + 45

            await _sut.EnsureValidAsync(
                null, EmployeeId, PrimaryServiceId, start, end,
                extraServiceIds: new[] { ExtraServiceId });
        }

        [Fact]
        public async Task EnsureValidAsync_Multiservicio_DuracionSoloDelPrincipal_Lanza()
        {
            ArrangeOpenDayWithServices(primaryMinutes: 30, extraMinutes: 45);
            var start = new DateTime(2030, 6, 3, 9, 0, 0);
            var end = start.AddMinutes(30); // ignores the extra -> total mismatch

            await Assert.ThrowsAsync<AppointmentDurationMismatchException>(() =>
                _sut.EnsureValidAsync(
                    null, EmployeeId, PrimaryServiceId, start, end,
                    extraServiceIds: new[] { ExtraServiceId }));
        }

        [Fact]
        public async Task EnsureValidAsync_ServicioExtraDeOtroNegocio_Lanza()
        {
            ArrangeOpenDayWithServices(primaryMinutes: 30, extraMinutes: 30);
            // The extra belongs to a different business than the employee.
            _serviceRepository.GetByIdAsync(ExtraServiceId)
                .Returns(new Service { Id = ExtraServiceId, BusinessId = TestIds.Of(9999), DurationMinutes = 30 });
            var start = new DateTime(2030, 6, 3, 9, 0, 0);
            var end = start.AddMinutes(60);

            await Assert.ThrowsAsync<ServiceEmployeeMismatchException>(() =>
                _sut.EnsureValidAsync(
                    null, EmployeeId, PrimaryServiceId, start, end,
                    extraServiceIds: new[] { ExtraServiceId }));
        }

        private void ArrangeOpenDayWithServices(int primaryMinutes, int extraMinutes)
        {
            _clock.BusinessNow.Returns(new DateTime(2030, 6, 1, 0, 0, 0));
            _employeeRepository.GetByIdAsync(EmployeeId)
                .Returns(new Employee { Id = EmployeeId, BusinessId = BusinessId, IsActive = true, MaxConcurrentAppointments = 1 });
            _businessRepository.GetByIdAsync(BusinessId).Returns(new Business { Id = BusinessId });
            _serviceRepository.GetByIdAsync(PrimaryServiceId)
                .Returns(new Service { Id = PrimaryServiceId, BusinessId = BusinessId, DurationMinutes = primaryMinutes });
            _serviceRepository.GetByIdAsync(ExtraServiceId)
                .Returns(new Service { Id = ExtraServiceId, BusinessId = BusinessId, DurationMinutes = extraMinutes });
            _scheduleResolver.GetEffectiveScheduleAsync(BusinessId, Arg.Any<DateOnly>())
                .Returns(new EffectiveSchedule
                {
                    IsOpen = true,
                    TimeSlots = new List<EffectiveTimeSlot>
                    {
                        new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(20, 0) }
                    }
                });
            _appointmentRepository.CountOverlappingForEmployeeAsync(
                    Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
                .Returns(0);
        }
    }
}
