using System.Security.Claims;

namespace MRC.Agendia.Api.Services
{
    /// <summary>
    /// Helpers to access the current user from the controllers.
    /// </summary>
    public static class CurrentUserService
    {
        public static string GetUserId(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("No hay usuario autenticado.");
    }
}
