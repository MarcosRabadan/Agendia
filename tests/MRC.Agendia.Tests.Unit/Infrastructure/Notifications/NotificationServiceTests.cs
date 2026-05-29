using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Application.Common.Push;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Notifications;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Notifications
{
    public class NotificationServiceTests
    {
        private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
        private readonly IWaitlistRepository _waitlist = Substitute.For<IWaitlistRepository>();
        private readonly IDeviceTokenRepository _deviceTokens = Substitute.For<IDeviceTokenRepository>();
        private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
        private readonly IPushSender _pushSender = Substitute.For<IPushSender>();
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _sut = new NotificationService(
                _appointments, _waitlist, _deviceTokens, _emailSender, _pushSender,
                Substitute.For<ILogger<NotificationService>>());
        }

        private static Appointment BuildAppointment(string? clientEmail = "ana@test.com", string? clientUserId = null) => new()
        {
            Id = 5,
            StartDate = new DateTime(2027, 1, 4, 9, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2027, 1, 4, 9, 30, 0, DateTimeKind.Utc),
            Client = new Client { Name = "Ana", Email = clientEmail, UserId = clientUserId },
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
        public async Task Confirmation_ConTokensDeDispositivo_EnviaPush()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>())
                .Returns(BuildAppointment(clientUserId: "user-1"));
            _deviceTokens.GetTokensByUserIdAsync("user-1", Arg.Any<CancellationToken>())
                .Returns(new List<string> { "tok-a", "tok-b" });

            await _sut.SendAppointmentConfirmationAsync(5);

            await _pushSender.Received(1).SendAsync(
                Arg.Is<IReadOnlyCollection<string>>(t => t.Count == 2),
                Arg.Is<string>(s => s.Contains("confirmada", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Confirmation_SiElPushFalla_NoRompeElEmail()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>())
                .Returns(BuildAppointment(clientUserId: "user-1"));
            _deviceTokens.GetTokensByUserIdAsync("user-1", Arg.Any<CancellationToken>())
                .Returns(new List<string> { "tok" });
            _pushSender.SendAsync(
                    Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("push provider down"));

            var handled = await _sut.SendAppointmentConfirmationAsync(5);

            // Push is best-effort: the email still goes out and the result is success.
            Assert.True(handled);
            await _emailSender.Received(1).SendAsync("ana@test.com", Arg.Any<string>(), Arg.Any<string>());
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

        [Fact]
        public async Task Reminder_CuandoEnvia_DevuelveTrue()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment());

            var handled = await _sut.SendAppointmentReminderAsync(5);

            Assert.True(handled);
        }

        [Fact]
        public async Task Reminder_SinEmail_DevuelveTrue_ParaNoReintentar()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment(clientEmail: null));

            var handled = await _sut.SendAppointmentReminderAsync(5);

            // Nothing to send and nothing to retry: the reminder is considered handled.
            Assert.True(handled);
        }

        [Fact]
        public async Task Reminder_CuandoElEnvioFalla_DevuelveFalse_ParaPermitirReintento()
        {
            _appointments.GetByIdWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns(BuildAppointment());
            _emailSender
                .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new InvalidOperationException("smtp down"));

            var handled = await _sut.SendAppointmentReminderAsync(5);

            Assert.False(handled);
        }
    }
}
