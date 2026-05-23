using AutoMapper;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for <see cref="AppointmentService.UpdateAsync"/> focused on when
    /// it re-runs scheduling validation. A status/notes-only change must NOT be
    /// re-validated (so a past appointment can be marked Completed/NoShow), while a
    /// reschedule MUST be.
    /// </summary>
    public class AppointmentServiceTests
    {
        private readonly IAppointmentRepository _repository = Substitute.For<IAppointmentRepository>();
        private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
        private readonly IAppointmentSchedulingValidator _validator = Substitute.For<IAppointmentSchedulingValidator>();
        private readonly IBookingConcurrencyGuard _bookingGuard = Substitute.For<IBookingConcurrencyGuard>();
        private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
        private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly AppointmentService _sut;

        public AppointmentServiceTests()
        {
            // In unit tests the guard just runs the critical section directly.
            _bookingGuard.ExecuteSerializedAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task<Appointment>>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task<Appointment>>>()());
            _bookingGuard.ExecuteSerializedAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task>>()());

            _sut = new AppointmentService(
                _repository, _clientRepository, _validator, _bookingGuard,
                _notificationService, _auditLogger, _unitOfWork, _mapper);
        }

        [Fact]
        public async Task CreateAsync_EjecutaLaSeccionCriticaDentroDelGuard()
        {
            _mapper.Map<Appointment>(Arg.Any<CreateAppointmentDto>()).Returns(new Appointment { Id = 11 });
            _mapper.Map<AppointmentDto>(Arg.Any<Appointment>()).Returns(ci => ToDto(ci.Arg<Appointment>()));

            var dto = new CreateAppointmentDto(
                ClientId: 1, EmployeeId: 2, ServiceId: 3,
                StartDate: new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                EndDate: new DateTime(2030, 1, 1, 9, 30, 0, DateTimeKind.Utc),
                Notes: null);

            await _sut.CreateAsync(dto);

            // The validate + insert must run inside the per-employee/day guard.
            await _bookingGuard.Received(1).ExecuteSerializedAsync(
                dto.EmployeeId, DateOnly.FromDateTime(dto.StartDate),
                Arg.Any<Func<Task<Appointment>>>(), Arg.Any<CancellationToken>());
            await _validator.Received(1).EnsureValidAsync(
                Arg.Any<int?>(), dto.ClientId, dto.EmployeeId, dto.ServiceId, dto.StartDate, dto.EndDate, Arg.Any<CancellationToken>());
            await _repository.Received(1).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_StatusOnlyChangeOnPastAppointment_DoesNotRevalidateScheduling()
        {
            var entity = PastAppointment();
            _repository.GetByIdAsync(entity.Id).Returns(entity);
            _mapper.Map<AppointmentDto>(Arg.Any<Appointment>()).Returns(ci => ToDto(ci.Arg<Appointment>()));

            // Same booking fields as the stored appointment; only the status changes.
            var dto = new UpdateAppointmentDto(
                entity.Id, entity.ClientId, entity.EmployeeId, entity.ServiceId,
                entity.StartDate, entity.EndDate, AppointmentStatus.Completed, Notes: null);

            await _sut.UpdateAsync(dto);

            await _validator.DidNotReceiveWithAnyArgs().EnsureValidAsync(
                default, default, default, default, default, default, default);
        }

        [Fact]
        public async Task UpdateAsync_Reschedule_RevalidatesScheduling()
        {
            var entity = PastAppointment();
            _repository.GetByIdAsync(entity.Id).Returns(entity);
            _mapper.Map<AppointmentDto>(Arg.Any<Appointment>()).Returns(ci => ToDto(ci.Arg<Appointment>()));

            // Moves the appointment to a different time -> must be re-validated.
            var dto = new UpdateAppointmentDto(
                entity.Id, entity.ClientId, entity.EmployeeId, entity.ServiceId,
                entity.StartDate.AddDays(1), entity.EndDate.AddDays(1), entity.Status, Notes: null);

            await _sut.UpdateAsync(dto);

            await _validator.Received(1).EnsureValidAsync(
                entity.Id, dto.ClientId, dto.EmployeeId, dto.ServiceId,
                dto.StartDate, dto.EndDate, Arg.Any<CancellationToken>());
        }

        private static Appointment PastAppointment() => new()
        {
            Id = 7,
            ClientId = 1,
            EmployeeId = 2,
            ServiceId = 3,
            StartDate = new DateTime(2020, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2020, 1, 1, 9, 30, 0, DateTimeKind.Utc),
            Status = AppointmentStatus.Confirmed,
        };

        private static AppointmentDto ToDto(Appointment a) =>
            new(a.Id, a.ClientId, a.EmployeeId, a.ServiceId, a.StartDate, a.EndDate, a.Status, a.Notes);
    }
}
