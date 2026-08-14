using AutoMapper;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Events;
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
        private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
        private readonly IBookingConcurrencyGuard _bookingGuard = Substitute.For<IBookingConcurrencyGuard>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly WaitlistService _sut;

        public WaitlistServiceTests()
        {
            _mapper.Map<WaitlistEntryDto>(Arg.Any<WaitlistEntry>()).Returns(ci => ToDto(ci.Arg<WaitlistEntry>()));
            // The guard just runs the critical section directly in unit tests.
            _bookingGuard.ExecuteSerializedAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task>>()());
            _sut = new WaitlistService(
                _repository, _availability, _appointmentRepository, _eventPublisher, _bookingGuard, _unitOfWork, _mapper);
        }

        private JoinWaitlistDto Dto() => new(BusinessId: 10, ServiceId: 3, Date: new DateOnly(2030, 6, 7), StartTime: new TimeOnly(16, 0), EmployeeId: 2);

        [Fact]
        public async Task JoinAsync_FranjaCompleta_CreaEntradaWaiting()
        {
            SlotCapacity(0);
            _repository.ExistsWaitingAsync(UserId, 10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>()).Returns(false);

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
            _repository.ExistsWaitingAsync(UserId, 10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>()).Returns(true);

            await Assert.ThrowsAsync<DuplicateWaitlistEntryException>(() => _sut.JoinAsync(Dto(), UserId));
        }

        [Fact]
        public async Task LeaveAsync_PropiaEntrada_LaCancela()
        {
            var entry = new WaitlistEntry { Id = 5, ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entry);

            await _sut.LeaveAsync(5, UserId);

            Assert.Equal(WaitlistStatus.Cancelled, entry.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LeaveAsync_EntradaAjena_Lanza403()
        {
            var entry = new WaitlistEntry { Id = 5, ClientUserId = "other-user", Status = WaitlistStatus.Waiting };
            _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entry);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LeaveAsync(5, UserId));
            Assert.Equal(WaitlistStatus.Waiting, entry.Status);
        }

        [Fact]
        public async Task LeaveAsync_NoExiste_Lanza404()
        {
            _repository.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((WaitlistEntry?)null);

            await Assert.ThrowsAsync<WaitlistEntryNotFoundException>(() => _sut.LeaveAsync(404, UserId));
        }

        [Fact]
        public async Task NotifyForFreedAppointment_AvisaAlPrimeroYLoMarcaNotified()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment
                {
                    Id = 50,
                    EmployeeId = 2,
                    ServiceId = 3,
                    StartDate = new DateTime(2030, 6, 7, 16, 0, 0),
                    Employee = new Employee { Id = 2, BusinessId = 10, Business = new Business { Id = 10, DefaultLanguage = "es" } }
                });
            var waiting = new WaitlistEntry { Id = 7, ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>())
                .Returns(waiting);
            SlotCapacity(1); // the freed slot now has room

            await _sut.NotifyForFreedAppointmentAsync(50);

            Assert.Equal(WaitlistStatus.Notified, waiting.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
            // A WaitlistSlotAvailable event is published (via the outbox) instead of an email.
            await _eventPublisher.Received(1).PublishAsync(
                Arg.Is<IIntegrationEvent>(e => e is WaitlistSlotAvailable && ((WaitlistSlotAvailable)e).WaitlistEntryId == 7),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SerializaElTriggerPorEmpleadoYDia()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment
                {
                    Id = 50,
                    EmployeeId = 2,
                    ServiceId = 3,
                    StartDate = new DateTime(2030, 6, 7, 16, 0, 0),
                    Employee = new Employee { Id = 2, BusinessId = 10, Business = new Business { Id = 10, DefaultLanguage = "es" } }
                });
            _repository.GetNextWaitingForSlotAsync(10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>())
                .Returns(new WaitlistEntry { Id = 7, ClientUserId = UserId, Status = WaitlistStatus.Waiting });
            SlotCapacity(1);

            await _sut.NotifyForFreedAppointmentAsync(50);

            // The select-recheck-notify-mark section ran inside the per-employee/day lock.
            await _bookingGuard.Received(1).ExecuteSerializedAsync(
                2, new DateOnly(2030, 6, 7), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SiFallaLaPublicacion_DejaEnWaiting()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = 50, EmployeeId = 2, ServiceId = 3, StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = 2, BusinessId = 10, Business = new Business { Id = 10, DefaultLanguage = "es" } } });
            var waiting = new WaitlistEntry { Id = 7, ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>())
                .Returns(waiting);
            SlotCapacity(1); // the slot has room, so publication is attempted
            // Enlisting the event fails: the entry must stay Waiting (not marked
            // Notified) so a later freed slot re-selects it - the trigger is best-effort
            // and must not leave a "notified with no event" half-state.
            _eventPublisher.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
                .Returns(_ => throw new InvalidOperationException("outbox down"));

            await _sut.NotifyForFreedAppointmentAsync(50);

            Assert.Equal(WaitlistStatus.Waiting, waiting.Status);
            await _unitOfWork.DidNotReceive().Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_FranjaSigueLlena_NoAvisa()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = 50, EmployeeId = 2, ServiceId = 3, StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = 2, BusinessId = 10, Business = new Business { Id = 10, DefaultLanguage = "es" } } });
            var waiting = new WaitlistEntry { Id = 7, ClientUserId = UserId, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>())
                .Returns(waiting);
            SlotCapacity(0); // the freed appointment did not actually open a seat (still full)

            await _sut.NotifyForFreedAppointmentAsync(50);

            // No false "hay hueco" notification, and the entry stays Waiting for a real opening.
            await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
            Assert.Equal(WaitlistStatus.Waiting, waiting.Status);
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SinEsperando_NoNotifica()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = 50, EmployeeId = 2, ServiceId = 3, StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = 2, BusinessId = 10, Business = new Business { Id = 10, DefaultLanguage = "es" } } });
            _repository.GetNextWaitingForSlotAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((WaitlistEntry?)null);

            await _sut.NotifyForFreedAppointmentAsync(50);

            await _eventPublisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
        }

        [Fact]
        public async Task NotifyForFreedAppointment_EsBestEffort_NoPropaga()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Appointment?>(_ => throw new InvalidOperationException("db down"));

            // Must not throw: the cancellation/deletion that triggered it has already happened.
            await _sut.NotifyForFreedAppointmentAsync(50);
        }

        private void SlotCapacity(int? capacity)
            => _availability.GetSlotCapacityAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Returns(capacity);

        private static WaitlistEntryDto ToDto(WaitlistEntry w)
            => new(w.Id, w.BusinessId, w.ServiceId, w.ClientUserId, w.EmployeeId, w.Date, w.StartTime, w.Status, w.CreatedAt);
    }
}
