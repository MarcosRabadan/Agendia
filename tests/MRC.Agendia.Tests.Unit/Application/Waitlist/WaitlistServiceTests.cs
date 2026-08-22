using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Waitlist
{
    /// <summary>
    /// Unit tests for <see cref="WaitlistService"/>: join validation (full slot only,
    /// no duplicates), leave ownership, and the freed-slot trigger (notify the first
    /// waiting client, best-effort).
    /// </summary>
    public class WaitlistServiceTests
    {
        private const string UserId = "user-1";
        private static readonly DateOnly Day = new(2030, 6, 7);
        private static readonly TimeOnly SlotTime = new(16, 0);

        private readonly IWaitlistRepository _repository = Substitute.For<IWaitlistRepository>();
        private readonly IAvailabilityService _availability = Substitute.For<IAvailabilityService>();
        private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
        private readonly IBookingConcurrencyGuard _bookingGuard = Substitute.For<IBookingConcurrencyGuard>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly WaitlistService _sut;

        public WaitlistServiceTests()
        {
            _clock.TimeZoneId.Returns("Europe/Madrid");
            _mapper.Map<WaitlistEntryDto>(Arg.Any<WaitlistEntry>()).Returns(ci => ToDto(ci.Arg<WaitlistEntry>()));
            // The guard just runs the critical section directly in unit tests.
            _bookingGuard.ExecuteSerializedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task>>()());
            _sut = NewService(new WaitlistOptions());
        }

        private WaitlistService NewService(WaitlistOptions options) => new(
            _repository, _availability, _appointmentRepository, _bookingGuard, _unitOfWork,
            NullLogger<WaitlistService>.Instance, _mapper, _clock, options);

        private JoinWaitlistDto Dto() => new(BusinessId: TestIds.Of(10), ServiceId: TestIds.Of(3), Date: new DateOnly(2030, 6, 7), StartTime: new TimeOnly(16, 0), EmployeeId: TestIds.Of(2));

        [Fact]
        public async Task JoinAsync_FranjaCompleta_CreaEntradaWaiting()
        {
            SlotCapacity(0);
            _repository.ExistsWaitingAsync(UserId, TestIds.Of(10), TestIds.Of(3), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), TestIds.Of(2), Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.JoinAsync(Dto(), UserId);

            Assert.Equal(WaitlistStatus.Waiting, result.Status);
            Assert.Equal(UserId, result.ClientUserId);
            await _repository.Received(1).AddAsync(Arg.Is<WaitlistEntry>(w => w.Status == WaitlistStatus.Waiting && w.ClientUserId == UserId), Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task JoinAsync_FranjaConHueco_LanzaSlotHasCapacity()
        {
            SlotCapacity(2);

            await Assert.ThrowsAsync<SlotHasCapacityException>(() => _sut.JoinAsync(Dto(), UserId));
            await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        }

        [Fact]
        public async Task JoinAsync_FranjaFueraDeHorario_Lanza()
        {
            SlotCapacity(null); // not a valid/open slot

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.JoinAsync(Dto(), UserId));
        }

        [Fact]
        public async Task JoinAsync_Duplicada_LanzaDuplicate()
        {
            SlotCapacity(0);
            _repository.ExistsWaitingAsync(UserId, TestIds.Of(10), TestIds.Of(3), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), TestIds.Of(2), Arg.Any<CancellationToken>()).Returns(true);

            await Assert.ThrowsAsync<DuplicateWaitlistEntryException>(() => _sut.JoinAsync(Dto(), UserId));
        }

        [Fact]
        public async Task LeaveAsync_PropiaEntrada_LaCancela()
        {
            var entry = new WaitlistEntry { Id = TestIds.Of(5), ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetByIdAsync(TestIds.Of(5), Arg.Any<CancellationToken>()).Returns(entry);

            await _sut.LeaveAsync(TestIds.Of(5), UserId);

            Assert.Equal(WaitlistStatus.Cancelled, entry.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LeaveAsync_EntradaAjena_Lanza403()
        {
            var entry = new WaitlistEntry { Id = TestIds.Of(5), ClientUserId = "other-user", Status = WaitlistStatus.Waiting };
            _repository.GetByIdAsync(TestIds.Of(5), Arg.Any<CancellationToken>()).Returns(entry);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LeaveAsync(TestIds.Of(5), UserId));
            Assert.Equal(WaitlistStatus.Waiting, entry.Status);
        }

        [Fact]
        public async Task LeaveAsync_NoExiste_Lanza404()
        {
            _repository.GetByIdAsync(TestIds.Of(404), Arg.Any<CancellationToken>()).Returns((WaitlistEntry?)null);

            await Assert.ThrowsAsync<WaitlistEntryNotFoundException>(() => _sut.LeaveAsync(TestIds.Of(404), UserId));
        }

        [Fact]
        public async Task NotifyForFreedAppointment_AvisaAlPrimeroYLoMarcaNotified()
        {
            FreedAppointment();
            var waiting = Waiting(TestIds.Of(7), SlotTime);
            Candidates(waiting);
            SlotCapacity(1); // the freed slot now has room

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            Assert.Equal(WaitlistStatus.Notified, waiting.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
            // A WaitlistSlotAvailable event is raised on the entry (enlisted into the outbox on save).
            Assert.Contains(waiting.DomainEvents, e => e is WaitlistSlotAvailable && ((WaitlistSlotAvailable)e).WaitlistEntryId == TestIds.Of(7));
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SerializaElTriggerPorEmpleadoYDia()
        {
            FreedAppointment();
            Candidates(Waiting(TestIds.Of(7), SlotTime));
            SlotCapacity(1);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // The select-recheck-notify-mark section ran inside the per-employee/day lock.
            await _bookingGuard.Received(1).ExecuteSerializedAsync(
                TestIds.Of(2), Day, Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_FranjaSigueLlena_NoAvisa()
        {
            FreedAppointment();
            var waiting = Waiting(TestIds.Of(7), SlotTime);
            Candidates(waiting);
            SlotCapacity(0); // the freed appointment did not actually open a seat (still full)

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // No false "there is a spot" notification, and the entry stays Waiting for a real opening.
            Assert.Empty(waiting.DomainEvents);
            Assert.Equal(WaitlistStatus.Waiting, waiting.Status);
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SinEsperando_NoNotifica()
        {
            FreedAppointment();
            Candidates();

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            await _unitOfWork.DidNotReceive().Save(Arg.Any<CancellationToken>());
        }

        // #350: joining the queue is allowed whenever the slot is full, and fullness is measured
        // by OVERLAP - so a 16:00-17:00 class leaves people legitimately waiting at 16:30. The
        // notification matched by exact start time, so those entries sat in a queue they could
        // never be called from. Note the times below are deliberately NOT the class's own: the
        // old tests all shared one constant, which is why none of them could see this.

        [Fact]
        public async Task NotifyForFreedAppointment_BuscaCandidatosPorSolape()
        {
            FreedAppointment();
            Candidates();

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // 16:00-17:00 with a 60 minute service: anybody starting after 15:00 and before
            // 17:00 is still running when the seat frees up.
            await _repository.Received(1).GetWaitingCandidatesForSlotAsync(
                TestIds.Of(10), TestIds.Of(3), Day, new TimeOnly(17, 0), new TimeOnly(15, 0),
                TestIds.Of(2), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// The window that frees is the APPOINTMENT's, the duration that shifts the lower bound is
        /// the SERVICE's, and they are not the same number when the booking carries extra services
        /// (#170). With a 60 minute class of a 60 minute service - which is what every other test
        /// here uses - passing the wrong one of the two would look identical.
        /// </summary>
        [Fact]
        public async Task NotifyForFreedAppointment_ClaseMasLargaQueElServicio_UsaLaVentanaEntera()
        {
            FreedAppointment(durationMinutes: 90, serviceDurationMinutes: 30);
            Candidates();

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // 16:00 + 90 minutes of booking, minus 30 minutes of service on the lower bound.
            await _repository.Received(1).GetWaitingCandidatesForSlotAsync(
                TestIds.Of(10), TestIds.Of(3), Day, new TimeOnly(17, 30), new TimeOnly(15, 30),
                TestIds.Of(2), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_AvisaAlQueEsperaEnUnaHoraQueSolapa()
        {
            FreedAppointment();
            var overlapping = Waiting(TestIds.Of(7), new TimeOnly(16, 30));
            Candidates(overlapping);
            SlotCapacity(1);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            Assert.Equal(WaitlistStatus.Notified, overlapping.Status);
            Assert.Single(overlapping.DomainEvents.OfType<WaitlistSlotAvailable>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SaltaAlCandidatoCuyaFranjaSigueLlena()
        {
            // The 16:30 window is still blocked by the next class; 16:00 is now free. Stopping at
            // the head of the queue would leave both of them without a notification.
            FreedAppointment();
            var blocked = Waiting(TestIds.Of(7), new TimeOnly(16, 30));
            var fits = Waiting(TestIds.Of(8), SlotTime);
            Candidates(blocked, fits);
            SlotCapacity(0);
            SlotCapacity(SlotTime, 1);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            Assert.Equal(WaitlistStatus.Waiting, blocked.Status);
            Assert.Equal(WaitlistStatus.Notified, fits.Status);
        }

        [Fact]
        public async Task NotifyForFreedAppointment_AvisaAUnoSolo()
        {
            // One freed seat is one seat: the rest of the queue keeps waiting.
            FreedAppointment();
            var first = Waiting(TestIds.Of(7), new TimeOnly(16, 30));
            var second = Waiting(TestIds.Of(8), SlotTime);
            Candidates(first, second);
            SlotCapacity(1);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            Assert.Equal(WaitlistStatus.Notified, first.Status);
            Assert.Equal(WaitlistStatus.Waiting, second.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_AcotaLosCandidatosExaminados()
        {
            // Each candidate costs a capacity read INSIDE the booking lock, so the walk is capped
            // by configuration rather than by however long the queue happens to be.
            var sut = NewService(new WaitlistOptions { NotifyCandidateLimit = 2 });
            FreedAppointment();
            Candidates();

            await sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            await _repository.Received(1).GetWaitingCandidatesForSlotAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly?>(),
                Arg.Any<TimeOnly?>(), Arg.Any<Guid>(), 2, Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task NotifyForFreedAppointment_TopeAbsurdoEnConfig_SigueMirandoAlMenosAUno(int configured)
        {
            // A zero in the config would become Take(0) and kill the whole feature in silence:
            // no notification, no error, nothing in the log. The clamp is what stops that, so it
            // gets a test of its own.
            var sut = NewService(new WaitlistOptions { NotifyCandidateLimit = configured });
            FreedAppointment();
            Candidates();

            await sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            await _repository.Received(1).GetWaitingCandidatesForSlotAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly?>(),
                Arg.Any<TimeOnly?>(), Arg.Any<Guid>(), 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_EsBestEffort_NoPropaga()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns<Appointment?>(_ => throw new InvalidOperationException("db down"));

            // Must not throw: the cancellation/deletion that triggered it has already happened.
            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));
        }

        private void SlotCapacity(int? capacity)
            => _availability.GetSlotCapacityAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
                .Returns(capacity);

        /// <summary>Capacity for ONE start time, on top of the blanket stub above.</summary>
        private void SlotCapacity(TimeOnly startTime, int? capacity)
            => _availability.GetSlotCapacityAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), startTime, Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
                .Returns(capacity);

        /// <summary>
        /// The freed class, as <c>GetByIdWithDetailsAsync</c> really returns it: with its end
        /// time and its Service, because working out which queued slots overlap needs both.
        /// </summary>
        /// <param name="durationMinutes">How long the BOOKING lasts (extras included).</param>
        /// <param name="serviceDurationMinutes">
        /// How long the SERVICE lasts, which is what every queued candidate's slot lasts. Defaults
        /// to the booking's length; pass a different one for the multiservice case.
        /// </param>
        private void FreedAppointment(int durationMinutes = 60, int? serviceDurationMinutes = null)
        {
            var start = Day.ToDateTime(SlotTime);
            _appointmentRepository.GetByIdWithDetailsAsync(TestIds.Of(50), Arg.Any<CancellationToken>())
                .Returns(new Appointment
                {
                    Id = TestIds.Of(50),
                    EmployeeId = TestIds.Of(2),
                    ServiceId = TestIds.Of(3),
                    StartDate = start,
                    EndDate = start.AddMinutes(durationMinutes),
                    Service = new Service { Id = TestIds.Of(3), DurationMinutes = serviceDurationMinutes ?? durationMinutes },
                    Employee = new Employee { Id = TestIds.Of(2), BusinessId = TestIds.Of(10), Business = new Business { Id = TestIds.Of(10), DefaultLanguage = "es" } }
                });
        }

        private void Candidates(params WaitlistEntry[] entries)
            => _repository.GetWaitingCandidatesForSlotAsync(
                    Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly?>(),
                    Arg.Any<TimeOnly?>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(entries);

        private static WaitlistEntry Waiting(Guid id, TimeOnly startTime) => new()
        {
            Id = id,
            BusinessId = TestIds.Of(10),
            ServiceId = TestIds.Of(3),
            EmployeeId = TestIds.Of(2),
            ClientUserId = UserId,
            Date = Day,
            StartTime = startTime,
            Status = WaitlistStatus.Waiting,
            CreatedAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        private static WaitlistEntryDto ToDto(WaitlistEntry w)
            => new(w.Id, w.BusinessId, w.ServiceId, w.ClientUserId, w.EmployeeId, w.Date, w.StartTime, w.Status, w.CreatedAt);
    }
}
