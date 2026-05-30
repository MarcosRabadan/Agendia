using System.Security.Claims;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Api.Services
{
    public class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUserContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        /// <inheritdoc />
        public string? UserId => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <inheritdoc />
        public string? IpAddress => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        /// <inheritdoc />
        public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <inheritdoc />
        public bool IsInRole(string role) => _accessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
