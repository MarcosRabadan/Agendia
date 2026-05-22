using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IClientRepository _clientRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenStore refreshTokenStore,
            IClientRepository clientRepository,
            IBusinessRepository businessRepository,
            IEmployeeRepository employeeRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenStore = refreshTokenStore;
            _clientRepository = clientRepository;
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        public async Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Client);

            // Crear la entidad Client asociada
            var client = new Client
            {
                Name = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                UserId = user.Id
            };
            await _clientRepository.AddAsync(client);
            await _unitOfWork.Save();

            await SendEmailConfirmationAsync(user);
            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.BusinessOwner);

            // Create the associated Business.
            var business = new Business
            {
                Name = dto.BusinessName,
                Description = dto.BusinessDescription,
                Address = dto.BusinessAddress,
                Phone = dto.BusinessPhone,
                Email = dto.BusinessEmail,
                IsActive = true,
                OwnerUserId = user.Id
            };
            await _businessRepository.AddAsync(business);
            await _unitOfWork.Save();

            // Also auto-create an Employee record for the owner so a solo
            // professional can start taking bookings immediately without an
            // extra setup step. MaxConcurrentAppointments defaults to 1; the
            // owner can change it from /api/employee.
            var ownerEmployee = new Employee
            {
                BusinessId = business.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                IsActive = true,
                UserId = user.Id,
                MaxConcurrentAppointments = 1
            };
            await _employeeRepository.AddAsync(ownerEmployee);
            await _unitOfWork.Save();

            await SendEmailConfirmationAsync(user);
            return await BuildAuthResponseAsync(user);
        }

        public async Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId)
        {
            // Validar que el negocio existe y que el usuario actual es el dueño
            var business = await _businessRepository.GetByIdAsync(dto.BusinessId)
                ?? throw new KeyNotFoundException($"Business with Id {dto.BusinessId} not found.");

            if (business.OwnerUserId != currentOwnerUserId)
                throw new UnauthorizedAccessException("Solo el dueno del negocio puede crear empleados.");

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.Phone,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Employee);

            var employee = new Employee
            {
                BusinessId = dto.BusinessId,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                IsActive = true,
                UserId = user.Id
            };
            await _employeeRepository.AddAsync(employee);
            await _unitOfWork.Save();

            await SendEmailConfirmationAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new AuthenticationException("Credenciales invalidas.");

            if (!user.IsActive)
                throw new AuthenticationException("La cuenta esta desactivada.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new AuthenticationException("Cuenta bloqueada temporalmente por demasiados intentos fallidos.");

            var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                throw new AuthenticationException("Credenciales invalidas.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            // Optional email-confirmation gate (off by default so existing flows
            // and tests are unaffected). When enabled, unconfirmed users cannot
            // log in until they follow the confirmation link sent at registration.
            if (_configuration.GetValue<bool>("Auth:RequireConfirmedEmail") && !user.EmailConfirmed)
                throw new AuthenticationException("Debes confirmar tu email antes de iniciar sesion.");

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
        {
            var stored = await _refreshTokenStore.GetByTokenAsync(refreshToken)
                ?? throw new AuthenticationException("Refresh token invalido.");

            if (!stored.IsActive)
                throw new AuthenticationException("Refresh token expirado o revocado.");

            var user = await _userManager.FindByIdAsync(stored.UserId)
                ?? throw new AuthenticationException("Usuario no encontrado.");

            if (!user.IsActive)
                throw new AuthenticationException("La cuenta esta desactivada.");

            // Rotacion: revocar el actual y emitir uno nuevo
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            stored.RevokedAt = DateTime.UtcNow;
            stored.ReplacedByToken = newRefreshToken;
            _refreshTokenStore.Update(stored);

            return await BuildAuthResponseAsync(user, newRefreshToken);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var stored = await _refreshTokenStore.GetByTokenAsync(refreshToken);
            if (stored is null || !stored.IsActive) return; // idempotente

            stored.RevokedAt = DateTime.UtcNow;
            _refreshTokenStore.Update(stored);
            await _refreshTokenStore.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // Anti-enumeration: never reveal whether the email exists. The
            // endpoint always returns success; we only send the email for an
            // existing, active account and stay silent otherwise.
            if (user is null || !user.IsActive)
                return;

            await SendPasswordResetAsync(user);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            // Uniform message so a missing user is indistinguishable from a bad token.
            if (user is null)
                throw new InvalidOperationException("Token de restablecimiento invalido o expirado.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                    throw new InvalidOperationException("Token de restablecimiento invalido o expirado.");
                // Surface password-policy failures (weak password, etc.).
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            // A successful reset proves account ownership, so clear any active
            // lockout to let the user sign in immediately.
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null)
                throw new InvalidOperationException("Token de confirmacion invalido o expirado.");

            var result = await _userManager.ConfirmEmailAsync(user, dto.Token);
            if (!result.Succeeded)
                throw new InvalidOperationException("Token de confirmacion invalido o expirado.");
        }

        public async Task<UserDto> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user, string? existingRefreshToken = null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (accessToken, accessExpires) = _jwtTokenService.GenerateAccessToken(
                user.Id, user.Email!, user.FullName, roles);

            string refreshTokenValue = existingRefreshToken ?? _jwtTokenService.GenerateRefreshToken();
            var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenDays"] ?? "7");
            var refreshExpires = DateTime.UtcNow.AddDays(refreshDays);

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = refreshExpires
            };
            await _refreshTokenStore.AddAsync(refreshToken);
            await _refreshTokenStore.SaveChangesAsync();

            var userDto = new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
            return new AuthResponseDto(accessToken, accessExpires, refreshTokenValue, refreshExpires, userDto);
        }

        private async Task SendEmailConfirmationAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = BuildFrontendLink("confirm-email",
                $"userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}");

            var body =
                $"<p>Hola {WebUtility.HtmlEncode(user.FullName)},</p>" +
                "<p>Confirma tu direccion de email para activar tu cuenta:</p>" +
                $"<p><a href=\"{link}\">Confirmar email</a></p>";

            await _emailSender.SendAsync(user.Email!, "Confirma tu email - Agendia", body);
        }

        private async Task SendPasswordResetAsync(ApplicationUser user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = BuildFrontendLink("reset-password",
                $"email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");

            var body =
                $"<p>Hola {WebUtility.HtmlEncode(user.FullName)},</p>" +
                "<p>Has solicitado restablecer tu contrasena. El enlace caduca en 1 hora:</p>" +
                $"<p><a href=\"{link}\">Restablecer contrasena</a></p>" +
                "<p>Si no has sido tu, ignora este correo.</p>";

            await _emailSender.SendAsync(user.Email!, "Restablecer contrasena - Agendia", body);
        }

        private string BuildFrontendLink(string path, string query)
        {
            var baseUrl = (_configuration["Email:FrontendBaseUrl"] ?? string.Empty).TrimEnd('/');
            return $"{baseUrl}/{path}?{query}";
        }
    }
}
