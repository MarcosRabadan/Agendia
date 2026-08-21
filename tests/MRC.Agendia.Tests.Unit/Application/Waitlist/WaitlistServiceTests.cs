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
            _sut = new WaitlistService(
                _repository, _availability, _appointmentRepository, _bookingGuard, _unitOfWork,
                NullLogger<WaitlistService>.Instance, _mapper, _clock, new WaitlistOptions());
        }

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
            _appointmentRepository.GetByIdWithDetailsAsync(TestIds.Of(50), Arg.Any<CancellationToken>())
                .Returns(new Appointment
                {
                    Id = TestIds.Of(50),
                    EmployeeId = TestIds.Of(2),
                    ServiceId = TestIds.Of(3),
                    StartDate = new DateTime(2030, 6, 7, 16, 0, 0),
                    Employee = new Employee { Id = TestIds.Of(2), BusinessId = TestIds.Of(10), Business = new Business { Id = TestIds.Of(10), DefaultLanguage = "es" } }
                });
            var waiting = new WaitlistEntry { Id = TestIds.Of(7), ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(TestIds.Of(10), TestIds.Of(3), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), TestIds.Of(2), Arg.Any<CancellationToken>())
                .Returns(waiting);
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
            _appointmentRepository.GetByIdWithDetailsAsync(TestIds.Of(50), Arg.Any<CancellationToken>())
                .Returns(new Appointment
                {
                    Id = TestIds.Of(50),
                    EmployeeId = TestIds.Of(2),
                    ServiceId = TestIds.Of(3),
                    StartDate = new DateTime(2030, 6, 7, 16, 0, 0),
                    Employee = new Employee { Id = TestIds.Of(2), BusinessId = TestIds.Of(10), Business = new Business { Id = TestIds.Of(10), DefaultLanguage = "es" } }
                });
            _repository.GetNextWaitingForSlotAsync(TestIds.Of(10), TestIds.Of(3), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), TestIds.Of(2), Arg.Any<CancellationToken>())
                .Returns(new WaitlistEntry { Id = TestIds.Of(7), ClientUserId = UserId, Status = WaitlistStatus.Waiting });
            SlotCapacity(1);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // The select-recheck-notify-mark section ran inside the per-employee/day lock.
            await _bookingGuard.Received(1).ExecuteSerializedAsync(
                TestIds.Of(2), new DateOnly(2030, 6, 7), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_FranjaSigueLlena_NoAvisa()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(TestIds.Of(50), Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = TestIds.Of(50), EmployeeId = TestIds.Of(2), ServiceId = TestIds.Of(3), StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = TestIds.Of(2), BusinessId = TestIds.Of(10), Business = new Business { Id = TestIds.Of(10), DefaultLanguage = "es" } } });
            var waiting = new WaitlistEntry { Id = TestIds.Of(7), ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(TestIds.Of(10), TestIds.Of(3), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), TestIds.Of(2), Arg.Any<CancellationToken>())
                .Returns(waiting);
            SlotCapacity(0); // the freed appointment did not actually open a seat (still full)

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            // No false "there is a spot" notification, and the entry stays Waiting for a real opening.
            Assert.Empty(waiting.DomainEvents);
            Assert.Equal(WaitlistStatus.Waiting, waiting.Status);
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SinEsperando_NoNotifica()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(TestIds.Of(50), Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = TestIds.Of(50), EmployeeId = TestIds.Of(2), ServiceId = TestIds.Of(3), StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = TestIds.Of(2), BusinessId = TestIds.Of(10), Business = new Business { Id = TestIds.Of(10), DefaultLanguage = "es" } } });
            _repository.GetNextWaitingForSlotAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((WaitlistEntry?)null);

            await _sut.NotifyForFreedAppointmentAsync(TestIds.Of(50));

            await _unitOfWork.DidNotReceive().Save(Arg.Any<CancellationToken>());
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

        private static WaitlistEntryDto ToDto(WaitlistEntry w)
            => new(w.Id, w.BusinessId, w.ServiceId, w.ClientUserId, w.EmployeeId, w.Date, w.StartTime, w.Status, w.CreatedAt);
    }
}
