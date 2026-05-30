using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Common.Email;

namespace MRC.Agendia.Infrastructure.Identity
{
    public class AuthEmailService : IAuthEmailService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthEmailService(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = BuildFrontendLink("confirm-email",
                $"userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}");

            var body =
                $"<p>Hola {WebUtility.HtmlEncode(user.FullName)},</p>" +
                "<p>Confirma tu direccion de email para activar tu cuenta:</p>" +
                $"<p><a href=\"{link}\">Confirmar email</a></p>";

            await _emailSender.SendAsync(user.Email!, "Confirma tu email - Agendia", body, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = BuildFrontendLink("reset-password",
                $"email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");

            var body =
                $"<p>Hola {WebUtility.HtmlEncode(user.FullName)},</p>" +
                "<p>Has solicitado restablecer tu contrasena. El enlace caduca en 1 hora:</p>" +
                $"<p><a href=\"{link}\">Restablecer contrasena</a></p>" +
                "<p>Si no has sido tu, ignora este correo.</p>";

            await _emailSender.SendAsync(user.Email!, "Restablecer contrasena - Agendia", body, cancellationToken);
        }

        private string BuildFrontendLink(string path, string query)
        {
            var baseUrl = (_configuration["Email:FrontendBaseUrl"] ?? string.Empty).TrimEnd('/');
            return $"{baseUrl}/{path}?{query}";
        }
    }
}
