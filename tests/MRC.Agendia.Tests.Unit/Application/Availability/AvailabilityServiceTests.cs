using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Availability
{
    /// <summary>
    /// Availability must not offer slots that already started for "today"
    /// (the scheduling validator would reject them as past) - issue BIZ-06.
    /// </summary>
    public class AvailabilityServiceTests
    {
        private const int BusinessId = 1;
        private const int ServiceId = 2;

        private readonly IBusinessRepository _businessRepository = Substitute.For<IBusinessRepository>();
        private readonly IServiceRepository _serviceRepository = Substitute.For<IServiceRepository>();
        private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
        private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
        private readonly IScheduleResolver _scheduleResolver = Substitute.For<IScheduleResolver>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly AvailabilityService _sut;

        public AvailabilityServiceTests()
        {
            _sut = new AvailabilityService(
                _businessRepository, _serviceRepository, _employeeRepository,
                _appointmentRepository, _scheduleResolver, _clock);
        }

        [Fact]
        public async Task GetAvailabilityAsync_HoyConHorasYaPasadas_OmiteLosHuecosAnterioresAAhora()
        {
            var date = new DateOnly(2030, 6, 3);

            _businessRepository.GetActiveByIdAsync(BusinessId).Returns(new Business());
            _serviceRepository.GetByIdPublicAsync(ServiceId)
                .Returns(new Service { Id = ServiceId, BusinessId = BusinessId, DurationMinutes = 30 });
            _employeeRepository.GetActiveByBusinessIdAsync(BusinessId)
                .Returns(new List<Employee> { new() { Id = 10, BusinessId = BusinessId, IsActive = true, MaxConcurrentAppointments = 1 } });
            _scheduleResolver.GetEffectiveScheduleAsync(BusinessId, date)
                .Returns(new EffectiveSchedule
                {
                    Date = date,
                    IsOpen = true,
                    TimeSlots = new List<EffectiveTimeSlot>
                    {
                        new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(20, 0) }
                    }
                });
            _appointmentRepository.GetByBusinessIdAndDateRangeAsync(BusinessId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(new List<Appointment>());

            // "Now" is midday, so the morning slots are already in the past.
            _clock.BusinessNow.Returns(new DateTime(2030, 6, 3, 12, 0, 0));

            var result = await _sut.GetAvailabilityAsync(BusinessId, date, ServiceId, employeeId: null, stepMinutes: 60);

            Assert.True(result.IsOpen);
            Assert.NotEmpty(result.Slots);
            Assert.All(result.Slots, s => Assert.True(s.StartTime >= new TimeOnly(12, 0)));
            Assert.Contains(result.Slots, s => s.StartTime == new TimeOnly(12, 0));
            Assert.DoesNotContain(result.Slots, s => s.StartTime == new TimeOnly(8, 0));
        }

        [Fact]
        public async Task GetAvailabilityAsync_Multiservicio_DimensionaLosHuecosPorLaDuracionTotal()
        {
            var date = new DateOnly(2030, 6, 3);
            const int ExtraServiceId = 3;

            _businessRepository.GetActiveByIdAsync(BusinessId).Returns(new Business());
            _serviceRepository.GetByIdPublicAsync(ServiceId)
                .Returns(new Service { Id = ServiceId, BusinessId = BusinessId, DurationMinutes = 30 });
            _serviceRepository.GetByIdPublicAsync(ExtraServiceId)
                .Returns(new Service { Id = ExtraServiceId, BusinessId = BusinessId, DurationMinutes = 30 });
            _employeeRepository.GetActiveByBusinessIdAsync(BusinessId)
                .Returns(new List<Employee> { new() { Id = 10, BusinessId = BusinessId, IsActive = true, MaxConcurrentAppointments = 1 } });
            _scheduleResolver.GetEffectiveScheduleAsync(BusinessId, date)
                .Returns(new EffectiveSchedule
                {
                    Date = date,
                    IsOpen = true,
                    TimeSlots = new List<EffectiveTimeSlot>
                    {
                        new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) }
                    }
                });
            _appointmentRepository.GetByBusinessIdAndDateRangeAsync(BusinessId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(new List<Appointment>());
            _clock.BusinessNow.Returns(new DateTime(2030, 6, 1, 0, 0, 0)); // before the queried date

            var result = await _sut.GetAvailabilityAsync(
                BusinessId, date, ServiceId, employeeId: null, stepMinutes: 30,
                extraServiceIds: new[] { ExtraServiceId });

            // 30 (primary) + 30 (extra) = 60-minute blocks.
            Assert.Equal(60, result.DurationMinutes);
            Assert.NotEmpty(result.Slots);
            Assert.All(result.Slots, s => Assert.Equal(60, (s.EndTime - s.StartTime).TotalMinutes));
            // Window 9-11, 60-min block, 30-min step: 9:00, 9:30, 10:00 fit; nothing after 10:00.
            Assert.Contains(result.Slots, s => s.StartTime == new TimeOnly(9, 0) && s.EndTime == new TimeOnly(10, 0));
            Assert.DoesNotContain(result.Slots, s => s.StartTime == new TimeOnly(10, 30));
        }
    }
}
