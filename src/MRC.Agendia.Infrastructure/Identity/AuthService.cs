using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Credential and account management: login, refresh-token rotation, logout,
    /// password change/reset and email confirmation. User registration lives in
    /// <see cref="IUserRegistrationService"/>; token/response building in
    /// <see cref="IAuthResponseFactory"/>; account emails in
    /// <see cref="IAuthEmailService"/>.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IConfiguration _configuration;
        private readonly IAuthResponseFactory _authResponseFactory;
        private readonly IAuthEmailService _authEmailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenStore refreshTokenStore,
            IConfiguration configuration,
            IAuthResponseFactory authResponseFactory,
            IAuthEmailService authEmailService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenStore = refreshTokenStore;
            _configuration = configuration;
            _authResponseFactory = authResponseFactory;
            _authEmailService = authEmailService;
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

            return await _authResponseFactory.CreateAsync(user);
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

            // Rotation: revoke the current token and issue a new one.
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            stored.RevokedAt = DateTime.UtcNow;
            stored.ReplacedByToken = newRefreshToken;
            _refreshTokenStore.Update(stored);

            return await _authResponseFactory.CreateAsync(user, newRefreshToken);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var stored = await _refreshTokenStore.GetByTokenAsync(refreshToken);
            if (stored is null || !stored.IsActive) return; // idempotent

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

            await _authEmailService.SendPasswordResetAsync(user);
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
    }
}
