using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Notifications;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Notifications
{
    public class NotificationServiceTests
    {
        private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
        private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _sut = new NotificationService(_appointments, _emailSender, Substitute.For<ILogger<NotificationService>>());
        }

        private static Appointment BuildAppointment(string? clientEmail = "ana@test.com") => new()
        {
            Id = 5,
            StartDate = new DateTime(2027, 1, 4, 9, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2027, 1, 4, 9, 30, 0, DateTimeKind.Utc),
            Client = new Client { Name = "Ana", Email = clientEmail },
            Service = new Service { Name = "Corte" },
            Employee = new Employee { FullName = "Luis", Business = new Business { Name = "Peluqueria X" } }
        };

        [Fact]
        public async Task Confirmation_EnviaEmailAlCliente_ConAsuntoDeConfirmacion()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment());

            await _sut.SendAppointmentConfirmationAsync(5);

            await _emailSender.Received(1).SendAsync(
                "ana@test.com",
                Arg.Is<string>(s => s.Contains("confirmada", StringComparison.OrdinalIgnoreCase)),
                Arg.Is<string>(b => b.Contains("Corte") && b.Contains("Luis")));
        }

        [Fact]
        public async Task Cancellation_EnviaEmail_ConAsuntoDeCancelacion()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment());

            await _sut.SendAppointmentCancellationAsync(5);

            await _emailSender.Received(1).SendAsync(
                "ana@test.com",
                Arg.Is<string>(s => s.Contains("cancelada", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<string>());
        }

        [Fact]
        public async Task SinEmailDelCliente_NoEnviaNada()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment(clientEmail: null));

            await _sut.SendAppointmentReminderAsync(5);

            await _emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task CitaInexistente_NoEnviaNada_NiLanza()
        {
            _appointments.GetByIdWithDetailsAsync(99, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

            await _sut.SendAppointmentConfirmationAsync(99);

            await _emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
