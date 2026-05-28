using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for <see cref="AppointmentDelayService"/>: it must notify only the
    /// upcoming appointments in the current continuous slot (a morning delay must not
    /// reach the afternoon shift), respect MaxAppointments, and do nothing on a
    /// closed day.
    /// </summary>
    public class AppointmentDelayServiceTests
    {
        private const int BusinessId = 10;

        private readonly IAppointmentRepository _repository = Substitute.For<IAppointmentRepository>();
        private readonly IScheduleResolver _scheduleResolver = Substitute.For<IScheduleResolver>();
        private readonly INotificationService _notifications = Substitute.For<INotificationService>();
        private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly AppointmentDelayService _sut;

        public AppointmentDelayServiceTests()
        {
            _sut = new AppointmentDelayService(_repository, _scheduleResolver, _notifications, _auditLogger, _clock);
        }

        [Fact]
        public async Task NotifyDelay_SoloAvisaCitasDelTramoActual_NoCruzaElDescanso()
        {
            SetNow(new DateTime(2030, 6, 3, 10, 0, 0));
            OpenWith(Slot(9, 14), Slot(16, 20)); // split shift
            ReturnCandidates(Appt(11, 11, 0), Appt(12, 12, 30), Appt(13, 17, 0));

            var result = await _sut.NotifyDelayAsync(BusinessId, new NotifyDelayDto(EmployeeId: null, DelayMinutes: 15, MaxAppointments: null));

            Assert.Equal(2, result.Notified); // 11:00 and 12:30, not the 17:00 (afternoon)
            await _notifications.Received(1).SendDelayNotificationAsync(11, 15, Arg.Any<CancellationToken>());
            await _notifications.Received(1).SendDelayNotificationAsync(12, 15, Arg.Any<CancellationToken>());
            await _notifications.DidNotReceive().SendDelayNotificationAsync(13, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyDelay_RespetaMaxAppointments()
        {
            SetNow(new DateTime(2030, 6, 3, 10, 0, 0));
            OpenWith(Slot(9, 14));
            ReturnCandidates(Appt(11, 11, 0), Appt(12, 12, 0), Appt(13, 13, 0));

            var result = await _sut.NotifyDelayAsync(BusinessId, new NotifyDelayDto(null, 10, MaxAppointments: 2));

            Assert.Equal(2, result.Notified);
            await _notifications.Received(1).SendDelayNotificationAsync(11, 10, Arg.Any<CancellationToken>());
            await _notifications.Received(1).SendDelayNotificationAsync(12, 10, Arg.Any<CancellationToken>());
            await _notifications.DidNotReceive().SendDelayNotificationAsync(13, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyDelay_AntesDeAbrir_UsaElPrimerTramo()
        {
            SetNow(new DateTime(2030, 6, 3, 8, 0, 0)); // before opening
            OpenWith(Slot(9, 14), Slot(16, 20));
            ReturnCandidates(Appt(11, 9, 30), Appt(12, 17, 0));

            var result = await _sut.NotifyDelayAsync(BusinessId, new NotifyDelayDto(null, 20, null));

            Assert.Equal(1, result.Notified); // only the morning one
            await _notifications.Received(1).SendDelayNotificationAsync(11, 20, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyDelay_DiaCerrado_NoNotificaNiConsultaCitas()
        {
            SetNow(new DateTime(2030, 6, 3, 10, 0, 0));
            _scheduleResolver.GetEffectiveScheduleAsync(BusinessId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new EffectiveSchedule { IsOpen = false });

            var result = await _sut.NotifyDelayAsync(BusinessId, new NotifyDelayDto(null, 15, null));

            Assert.Equal(0, result.Notified);
            await _repository.DidNotReceiveWithAnyArgs().GetUpcomingForDelayAsync(default, default, default, default, default);
            await _notifications.DidNotReceiveWithAnyArgs().SendDelayNotificationAsync(default, default, default);
        }

        [Fact]
        public async Task NotifyDelay_FueraDeTodoTramo_NoNotifica()
        {
            SetNow(new DateTime(2030, 6, 3, 21, 0, 0)); // after the last slot ends
            OpenWith(Slot(9, 14), Slot(16, 20));

            var result = await _sut.NotifyDelayAsync(BusinessId, new NotifyDelayDto(null, 15, null));

            Assert.Equal(0, result.Notified);
            await _notifications.DidNotReceiveWithAnyArgs().SendDelayNotificationAsync(default, default, default);
        }

        private void SetNow(DateTime now) => _clock.BusinessNow.Returns(now);

        private void OpenWith(params EffectiveTimeSlot[] slots)
            => _scheduleResolver.GetEffectiveScheduleAsync(BusinessId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new EffectiveSchedule { IsOpen = true, TimeSlots = slots.ToList() });

        private void ReturnCandidates(params Appointment[] appts)
            => _repository.GetUpcomingForDelayAsync(BusinessId, Arg.Any<int?>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(appts.ToList());

        private static EffectiveTimeSlot Slot(int startHour, int endHour)
            => new() { StartTime = new TimeOnly(startHour, 0), EndTime = new TimeOnly(endHour, 0) };

        private static Appointment Appt(int id, int hour, int minute) => new()
        {
            Id = id,
            ClientId = 1,
            EmployeeId = 2,
            ServiceId = 3,
            StartDate = new DateTime(2030, 6, 3, hour, minute, 0),
            EndDate = new DateTime(2030, 6, 3, hour, minute, 0).AddMinutes(30),
            Status = AppointmentStatus.Confirmed,
        };
    }
}
