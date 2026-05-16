using System.Security.Claims;

namespace MRC.Agendia.Api.Services
{
    /// <summary>
    /// Helpers para acceder al usuario actual desde los controllers.
    /// </summary>
    public static class CurrentUserService
    {
        public static string GetUserId(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("No hay usuario autenticado.");

        public static string? GetEmail(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Email);

        public static bool IsInRole(this ClaimsPrincipal user, string role)
            => user.IsInRole(role);
    }
}
