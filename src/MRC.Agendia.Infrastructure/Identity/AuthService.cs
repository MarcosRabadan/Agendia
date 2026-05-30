using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Domain.Constants;
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
        private readonly IAuditLogger _auditLogger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<ApplicationUser> userManager,
                           IJwtTokenService jwtTokenService,
                           IRefreshTokenStore refreshTokenStore,
                           IConfiguration configuration,
                           IAuthResponseFactory authResponseFactory,
                           IAuthEmailService authEmailService,
                           IAuditLogger auditLogger,
                           IServiceScopeFactory scopeFactory,
                           ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenStore = refreshTokenStore;
            _configuration = configuration;
            _authResponseFactory = authResponseFactory;
            _authEmailService = authEmailService;
            _auditLogger = auditLogger;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                await _auditLogger.LogAsync(AuditActions.LoginFailed, "User", null, new { dto.Email, reason = "not_found" }, cancellationToken);
                throw new AuthenticationException("Credenciales invalidas.");
            }

            if (!user.IsActive)
            {
                await _auditLogger.LogAsync(AuditActions.LoginFailed, "User", user.Id, new { dto.Email, reason = "inactive" }, cancellationToken);
                throw new AuthenticationException("La cuenta esta desactivada.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _auditLogger.LogAsync(AuditActions.LoginFailed, "User", user.Id, new { dto.Email, reason = "locked" }, cancellationToken);
                throw new AuthenticationException("Cuenta bloqueada temporalmente por demasiados intentos fallidos.");
            }

            var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                await _auditLogger.LogAsync(AuditActions.LoginFailed, "User", user.Id, new { dto.Email, reason = "bad_password" }, cancellationToken);
                throw new AuthenticationException("Credenciales invalidas.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            // Optional email-confirmation gate (off by default so existing flows
            // and tests are unaffected). When enabled, unconfirmed users cannot
            // log in until they follow the confirmation link sent at registration.
            if (_configuration.GetValue<bool>("Auth:RequireConfirmedEmail") && !user.EmailConfirmed)
            {
                await _auditLogger.LogAsync(AuditActions.LoginFailed, "User", user.Id, new { dto.Email, reason = "email_not_confirmed" }, cancellationToken);
                throw new AuthenticationException("Debes confirmar tu email antes de iniciar sesion.");
            }

            await _auditLogger.LogAsync(AuditActions.LoginSuccess, "User", user.Id, new { dto.Email }, cancellationToken);
            return await _authResponseFactory.CreateAsync(user, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var stored = await _refreshTokenStore.GetByTokenAsync(refreshToken, cancellationToken)
                ?? throw new AuthenticationException("Refresh token invalido.");

            // Reuse detection: a token that was already rotated (revoked AND
            // replaced) is being presented again - the signature of a replayed or
            // stolen token. Revoke the whole family of sessions for the user.
            // Plain revoked tokens (e.g. from logout) have no ReplacedByToken and
            // fall through to the normal "revoked" path below.
            if (stored.RevokedAt is not null && stored.ReplacedByToken is not null)
            {
                await RevokeAllSessionsAsync(stored.UserId, cancellationToken);
                throw new AuthenticationException("Refresh token expirado o revocado.");
            }

            if (!stored.IsActive)
                throw new AuthenticationException("Refresh token expirado o revocado.");

            var user = await _userManager.FindByIdAsync(stored.UserId)
                ?? throw new AuthenticationException("Usuario no encontrado.");

            if (!user.IsActive)
                throw new AuthenticationException("La cuenta esta desactivada.");

            // Rotation: revoke the current token and issue a new one.
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            stored.RevokedAt = DateTime.UtcNow;
            // ReplacedByToken only flags that this token was rotated (reuse detection
            // null-checks it), so store the hash too - never the cleartext.
            stored.ReplacedByToken = RefreshTokenHasher.Hash(newRefreshToken);
            _refreshTokenStore.Update(stored);

            return await _authResponseFactory.CreateAsync(user, newRefreshToken, cancellationToken);
        }

        /// <inheritdoc />
        public async Task LogoutAsync(string refreshToken, string userId, CancellationToken cancellationToken = default)
        {
            var stored = await _refreshTokenStore.GetByTokenAsync(refreshToken, cancellationToken);
            // Idempotent, and a caller may only revoke their OWN token: ignore
            // unknown/inactive tokens and tokens that belong to another user.
            if (stored is null || !stored.IsActive || stored.UserId != userId) return;

            stored.RevokedAt = DateTime.UtcNow;
            _refreshTokenStore.Update(stored);
            await _refreshTokenStore.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default)
        {
            await RevokeAllSessionsAsync(userId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            // Changing the password invalidates every other session.
            await RevokeAllSessionsAsync(userId, cancellationToken);

            await _auditLogger.LogAsync(AuditActions.PasswordChanged, "User", userId, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
        {
            // Anti-enumeration: respond in constant time regardless of whether the
            // account exists. Doing the lookup + token + SMTP inline would make an
            // existing account measurably slower than a missing one (a timing
            // oracle) and surface a 500 only for existing accounts on SMTP failure.
            // The work runs best-effort in its own DI scope so the request returns
            // immediately (and always 204 upstream).
            var email = dto.Email;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IAuthEmailService>();

                    var user = await userManager.FindByEmailAsync(email);
                    if (user is not null && user.IsActive)
                        await emailService.SendPasswordResetAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando el email de restablecimiento de contrasena (best-effort).");
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
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

            // A reset is a compromise-recovery signal: invalidate every session.
            await RevokeAllSessionsAsync(user.Id, cancellationToken);

            await _auditLogger.LogAsync(AuditActions.PasswordReset, "User", user.Id, new { dto.Email }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            // Re-check IsActive for consistency with login; the uniform message also keeps
            // a missing/inactive account indistinguishable from a bad token.
            if (user is null || !user.IsActive)
                throw new InvalidOperationException("Token de confirmacion invalido o expirado.");

            var result = await _userManager.ConfirmEmailAsync(user, dto.Token);
            if (!result.Succeeded)
                throw new InvalidOperationException("Token de confirmacion invalido o expirado.");
        }

        /// <inheritdoc />
        public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
        }

        /// <summary>Revokes every active refresh token for the user. Idempotent.</summary>
        private async Task RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _refreshTokenStore.GetActiveByUserIdAsync(userId, cancellationToken);
            if (tokens.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
                _refreshTokenStore.Update(token);
            }
            await _refreshTokenStore.SaveChangesAsync(cancellationToken);
        }
    }
}
