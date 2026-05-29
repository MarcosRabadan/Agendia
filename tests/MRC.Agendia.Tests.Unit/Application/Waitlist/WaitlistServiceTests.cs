using AutoMapper;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
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
        private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
        private readonly IAvailabilityService _availability = Substitute.For<IAvailabilityService>();
        private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
        private readonly INotificationService _notifications = Substitute.For<INotificationService>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly WaitlistService _sut;

        public WaitlistServiceTests()
        {
            _clientRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                .Returns(new Client { Id = 1, Name = "Ana", UserId = UserId });
            _mapper.Map<WaitlistEntryDto>(Arg.Any<WaitlistEntry>()).Returns(ci => ToDto(ci.Arg<WaitlistEntry>()));
            _sut = new WaitlistService(
                _repository, _clientRepository, _availability, _appointmentRepository, _notifications, _unitOfWork, _mapper);
        }

        private JoinWaitlistDto Dto() => new(BusinessId: 10, ServiceId: 3, Date: new DateOnly(2030, 6, 7), StartTime: new TimeOnly(16, 0), EmployeeId: 2);

        [Fact]
        public async Task JoinAsync_FranjaCompleta_CreaEntradaWaiting()
        {
            SlotCapacity(0);
            _repository.ExistsWaitingAsync(1, 10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>()).Returns(false);

            var result = await _sut.JoinAsync(Dto(), UserId);

            Assert.Equal(WaitlistStatus.Waiting, result.Status);
            Assert.Equal(1, result.ClientId);
            await _repository.Received(1).AddAsync(Arg.Is<WaitlistEntry>(w => w.Status == WaitlistStatus.Waiting && w.ClientId == 1), Arg.Any<CancellationToken>());
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
            _repository.ExistsWaitingAsync(1, 10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>()).Returns(true);

            await Assert.ThrowsAsync<DuplicateWaitlistEntryException>(() => _sut.JoinAsync(Dto(), UserId));
        }

        [Fact]
        public async Task JoinAsync_SinCliente_Lanza403()
        {
            _clientRepository.GetByUserIdAsync("ghost", Arg.Any<CancellationToken>()).Returns((Client?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.JoinAsync(Dto(), "ghost"));
        }

        [Fact]
        public async Task LeaveAsync_PropiaEntrada_LaCancela()
        {
            var entry = new WaitlistEntry { Id = 5, ClientId = 1, Status = WaitlistStatus.Waiting };
            _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entry);

            await _sut.LeaveAsync(5, UserId);

            Assert.Equal(WaitlistStatus.Cancelled, entry.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LeaveAsync_EntradaAjena_Lanza403()
        {
            var entry = new WaitlistEntry { Id = 5, ClientId = 999, Status = WaitlistStatus.Waiting };
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
                    Employee = new Employee { Id = 2, BusinessId = 10 }
                });
            var waiting = new WaitlistEntry { Id = 7, ClientId = 1, Status = WaitlistStatus.Waiting };
            _repository.GetNextWaitingForSlotAsync(10, 3, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), 2, Arg.Any<CancellationToken>())
                .Returns(waiting);

            await _sut.NotifyForFreedAppointmentAsync(50);

            Assert.Equal(WaitlistStatus.Notified, waiting.Status);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
            await _notifications.Received(1).SendWaitlistAvailabilityAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task NotifyForFreedAppointment_SinEsperando_NoNotifica()
        {
            _appointmentRepository.GetByIdWithDetailsAsync(50, Arg.Any<CancellationToken>())
                .Returns(new Appointment { Id = 50, EmployeeId = 2, ServiceId = 3, StartDate = new DateTime(2030, 6, 7, 16, 0, 0), Employee = new Employee { Id = 2, BusinessId = 10 } });
            _repository.GetNextWaitingForSlotAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((WaitlistEntry?)null);

            await _sut.NotifyForFreedAppointmentAsync(50);

            await _notifications.DidNotReceiveWithAnyArgs().SendWaitlistAvailabilityAsync(default, default);
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
            => new(w.Id, w.BusinessId, w.ServiceId, w.ClientId, w.EmployeeId, w.Date, w.StartTime, w.Status, w.CreatedAt);
    }
}
