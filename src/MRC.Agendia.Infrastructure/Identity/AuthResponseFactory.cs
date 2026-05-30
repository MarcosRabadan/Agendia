using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Auth;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Infrastructure.Identity
{
    public class AuthResponseFactory : IAuthResponseFactory
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IConfiguration _configuration;

        public AuthResponseFactory(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenStore refreshTokenStore,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _refreshTokenStore = refreshTokenStore;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> CreateAsync(ApplicationUser user, string? existingRefreshToken = null, CancellationToken cancellationToken = default)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (accessToken, accessExpires) = _jwtTokenService.GenerateAccessToken(
                user.Id, user.Email!, user.FullName, roles);

            string refreshTokenValue = existingRefreshToken ?? _jwtTokenService.GenerateRefreshToken();
            var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var parsedDays) ? parsedDays : 7;
            var refreshExpires = DateTime.UtcNow.AddDays(refreshDays);

            var refreshToken = new RefreshToken
            {
                // Store only the hash; the cleartext value is returned to the client
                // below, so a DB leak exposes no reusable tokens.
                Token = RefreshTokenHasher.Hash(refreshTokenValue),
                UserId = user.Id,
                ExpiresAt = refreshExpires
            };
            await _refreshTokenStore.AddAsync(refreshToken, cancellationToken);
            await _refreshTokenStore.SaveChangesAsync(cancellationToken);

            var userDto = new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
            return new AuthResponseDto(accessToken, accessExpires, refreshTokenValue, refreshExpires, userDto);
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> CreateWithoutSessionAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto(user.Id, user.Email!, user.FullName, user.PhoneNumber, user.IsActive, roles);
            return new AuthResponseDto(string.Empty, default, string.Empty, default, userDto);
        }
    }
}
