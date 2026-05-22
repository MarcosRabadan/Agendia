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

        public async Task<AuthResponseDto> CreateAsync(ApplicationUser user, string? existingRefreshToken = null)
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
    }
}
