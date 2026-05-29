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
        private const int BusinessId = 1;
        private const int EmployeeId = 10;
        private const int ClientId = 5;
        private const int PrimaryServiceId = 100;
        private const int ExtraServiceId = 200;

        private readonly IBusinessRepository _businessRepository = Substitute.For<IBusinessRepository>();
        private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
        private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
        private readonly IServiceRepository _serviceRepository = Substitute.For<IServiceRepository>();
        private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
        private readonly IScheduleResolver _scheduleResolver = Substitute.For<IScheduleResolver>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly AppointmentSchedulingValidator _sut;

        public AppointmentSchedulingValidatorTests()
        {
            _sut = new AppointmentSchedulingValidator(
                _businessRepository, _clientRepository, _employeeRepository,
                _serviceRepository, _appointmentRepository, _scheduleResolver, _clock);
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

        [Fact]
        public async Task EnsureValidAsync_Multiservicio_DuracionTotalCorrecta_NoLanza()
        {
            ArrangeOpenDayWithServices(primaryMinutes: 30, extraMinutes: 45);
            var start = new DateTime(2030, 6, 3, 9, 0, 0);
            var end = start.AddMinutes(75); // 30 + 45

            await _sut.EnsureValidAsync(
                null, ClientId, EmployeeId, PrimaryServiceId, start, end,
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
                    null, ClientId, EmployeeId, PrimaryServiceId, start, end,
                    extraServiceIds: new[] { ExtraServiceId }));
        }

        [Fact]
        public async Task EnsureValidAsync_ServicioExtraDeOtroNegocio_Lanza()
        {
            ArrangeOpenDayWithServices(primaryMinutes: 30, extraMinutes: 30);
            // The extra belongs to a different business than the employee.
            _serviceRepository.GetByIdAsync(ExtraServiceId)
                .Returns(new Service { Id = ExtraServiceId, BusinessId = BusinessId + 99, DurationMinutes = 30 });
            var start = new DateTime(2030, 6, 3, 9, 0, 0);
            var end = start.AddMinutes(60);

            await Assert.ThrowsAsync<ServiceEmployeeMismatchException>(() =>
                _sut.EnsureValidAsync(
                    null, ClientId, EmployeeId, PrimaryServiceId, start, end,
                    extraServiceIds: new[] { ExtraServiceId }));
        }

        private void ArrangeOpenDayWithServices(int primaryMinutes, int extraMinutes)
        {
            _clock.BusinessNow.Returns(new DateTime(2030, 6, 1, 0, 0, 0));
            _clientRepository.GetByIdAsync(ClientId).Returns(new Client { Id = ClientId });
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
                    Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Returns(0);
        }
    }
}
