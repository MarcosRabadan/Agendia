using AutoMapper;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for <see cref="RecurringAppointmentService"/>: generation reuses
    /// the scheduling validator + booking guard, conflicting occurrences are skipped
    /// (not aborted), and cancel/delete/move only touch the right rows.
    /// </summary>
    public class RecurringAppointmentServiceTests
    {
        private readonly IServiceRepository _serviceRepository = Substitute.For<IServiceRepository>();
        private readonly IAppointmentRepository _repository = Substitute.For<IAppointmentRepository>();
        private readonly IAppointmentSchedulingValidator _validator = Substitute.For<IAppointmentSchedulingValidator>();
        private readonly IBookingConcurrencyGuard _bookingGuard = Substitute.For<IBookingConcurrencyGuard>();
        private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly RecurringAppointmentService _sut;

        public RecurringAppointmentServiceTests()
        {
            // The guard just runs the critical section directly in unit tests.
            _bookingGuard.ExecuteSerializedAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task<Appointment>>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task<Appointment>>>()());
            _bookingGuard.ExecuteSerializedAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task>>()());

            _clock.BusinessNow.Returns(new DateTime(2030, 1, 1));
            _serviceRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new Service { Id = 3, BusinessId = 10, DurationMinutes = 30 });
            _mapper.Map<AppointmentDto>(Arg.Any<Appointment>()).Returns(ci => ToDto(ci.Arg<Appointment>()));

            _sut = new RecurringAppointmentService(
                _serviceRepository, _repository, _validator, _bookingGuard,
                _auditLogger, _clock, _unitOfWork, _mapper);
        }

        [Fact]
        public async Task CreateSeriesAsync_Weekly_CreaUnaCitaPorOcurrencia_ConMismoSeriesId()
        {
            var start = new DateOnly(2030, 1, 7);
            var dto = WeeklySeries(start, until: start.AddDays(14)); // day 0, 7, 14

            var result = await _sut.CreateSeriesAsync(dto);

            Assert.Equal(3, result.Created.Count);
            Assert.Empty(result.Skipped);
            Assert.NotEqual(Guid.Empty, result.SeriesId);
            Assert.All(result.Created, c => Assert.Equal(result.SeriesId, c.SeriesId));
            Assert.All(result.Created, c => Assert.Equal(new TimeOnly(16, 0), TimeOnly.FromDateTime(c.StartDate)));
            await _repository.Received(3).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateSeriesAsync_OcurrenciaEnConflicto_SeSaltaYSeReporta()
        {
            var start = new DateOnly(2030, 1, 7);
            var dto = WeeklySeries(start, until: start.AddDays(21)); // 4 occurrences

            // The 2nd occurrence is full; the rest fit.
            _validator.EnsureValidAsync(
                    Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
                    Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
                .Returns(
                    Task.CompletedTask,
                    Task.FromException(new AppointmentConflictException("El empleado ya tiene otra cita.")),
                    Task.CompletedTask,
                    Task.CompletedTask);

            var result = await _sut.CreateSeriesAsync(dto);

            Assert.Equal(3, result.Created.Count);
            var skip = Assert.Single(result.Skipped);
            Assert.Equal("APPOINTMENT_CONFLICT", skip.Code);
            await _repository.Received(3).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateSeriesAsync_ServicioInexistente_Lanza()
        {
            _serviceRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((Service?)null);

            var dto = WeeklySeries(new DateOnly(2030, 1, 7), until: new DateOnly(2030, 1, 21));

            await Assert.ThrowsAsync<ServiceNotFoundException>(() => _sut.CreateSeriesAsync(dto));
        }

        [Fact]
        public async Task CancelSeriesAsync_CancelaSoloFuturasActivas()
        {
            var seriesId = Guid.NewGuid();
            var past = Appt(1, new DateTime(2029, 12, 31, 10, 0, 0), AppointmentStatus.Confirmed, seriesId);
            var futurePending = Appt(2, new DateTime(2030, 2, 1, 10, 0, 0), AppointmentStatus.Pending, seriesId);
            var futureConfirmed = Appt(3, new DateTime(2030, 2, 2, 10, 0, 0), AppointmentStatus.Confirmed, seriesId);
            var futureCancelled = Appt(4, new DateTime(2030, 2, 3, 10, 0, 0), AppointmentStatus.Cancelled, seriesId);
            var futureCompleted = Appt(5, new DateTime(2030, 2, 4, 10, 0, 0), AppointmentStatus.Completed, seriesId);
            _repository.GetBySeriesIdAsync(seriesId, Arg.Any<CancellationToken>())
                .Returns(new List<Appointment> { past, futurePending, futureConfirmed, futureCancelled, futureCompleted });

            var result = await _sut.CancelSeriesAsync(seriesId);

            Assert.Equal(2, result.Affected);
            Assert.Equal(AppointmentStatus.Cancelled, futurePending.Status);
            Assert.Equal(AppointmentStatus.Cancelled, futureConfirmed.Status);
            Assert.Equal(AppointmentStatus.Confirmed, past.Status);       // untouched (past)
            Assert.Equal(AppointmentStatus.Completed, futureCompleted.Status); // untouched (terminal)
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteSeriesAsync_BorraTodasLasDeLaSerie()
        {
            var seriesId = Guid.NewGuid();
            var list = new List<Appointment>
            {
                Appt(1, new DateTime(2029, 1, 1, 10, 0, 0), AppointmentStatus.Completed, seriesId),
                Appt(2, new DateTime(2030, 2, 1, 10, 0, 0), AppointmentStatus.Pending, seriesId),
                Appt(3, new DateTime(2030, 2, 8, 10, 0, 0), AppointmentStatus.Confirmed, seriesId),
            };
            _repository.GetBySeriesIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(list);

            var result = await _sut.DeleteSeriesAsync(seriesId);

            Assert.Equal(3, result.Affected);
            _repository.Received(3).Delete(Arg.Any<Appointment>());
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task MoveSeriesAsync_DesplazaFuturas_YSaltaLasQueChocan()
        {
            var seriesId = Guid.NewGuid();
            var a1 = Appt(1, new DateTime(2030, 2, 1, 10, 0, 0), AppointmentStatus.Confirmed, seriesId);
            a1.EndDate = new DateTime(2030, 2, 1, 10, 30, 0);
            var a2 = Appt(2, new DateTime(2030, 2, 8, 10, 0, 0), AppointmentStatus.Confirmed, seriesId);
            a2.EndDate = new DateTime(2030, 2, 8, 10, 30, 0);
            _repository.GetBySeriesIdAsync(seriesId, Arg.Any<CancellationToken>())
                .Returns(new List<Appointment> { a1, a2 });

            // First moves fine, second hits a closed day.
            _validator.EnsureValidAsync(
                    Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
                    Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
                .Returns(
                    Task.CompletedTask,
                    Task.FromException(new AppointmentOutsideScheduleException("El negocio esta cerrado.")));

            var result = await _sut.MoveSeriesAsync(seriesId, new MoveAppointmentSeriesDto(NewStartTime: null, DayShift: 7));

            Assert.Single(result.Moved);
            var skip = Assert.Single(result.Skipped);
            Assert.Equal("APPOINTMENT_OUTSIDE_SCHEDULE", skip.Code);
            // a1 shifted +7 days and its reminder re-armed; a2 left untouched.
            Assert.Equal(new DateTime(2030, 2, 8, 10, 0, 0), a1.StartDate);
            Assert.Null(a1.ReminderSentAt);
            Assert.Equal(new DateTime(2030, 2, 8, 10, 0, 0), a2.StartDate);
        }

        [Fact]
        public async Task CancelSeriesAsync_SerieInexistente_Lanza()
        {
            _repository.GetBySeriesIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(new List<Appointment>());

            await Assert.ThrowsAsync<AppointmentSeriesNotFoundException>(() => _sut.CancelSeriesAsync(Guid.NewGuid()));
        }

        private static CreateAppointmentSeriesDto WeeklySeries(DateOnly start, DateOnly until) => new(
            ClientId: 1, EmployeeId: 2, ServiceId: 3,
            StartTime: new TimeOnly(16, 0),
            Frequency: RecurrenceFrequency.Weekly, Interval: 1,
            DaysOfWeek: new[] { start.DayOfWeek }, DayOfMonth: null,
            StartDate: start, UntilDate: until, Notes: "Clase");

        private static Appointment Appt(int id, DateTime start, AppointmentStatus status, Guid seriesId) => new()
        {
            Id = id,
            ClientId = 1,
            EmployeeId = 2,
            ServiceId = 3,
            StartDate = start,
            EndDate = start.AddMinutes(30),
            Status = status,
            SeriesId = seriesId,
            ReminderSentAt = new DateTime(2029, 1, 1),
        };

        private static AppointmentDto ToDto(Appointment a) =>
            new(a.Id, a.ClientId, a.EmployeeId, a.ServiceId, a.StartDate, a.EndDate, a.Status, a.Notes, a.SeriesId);
    }
}
