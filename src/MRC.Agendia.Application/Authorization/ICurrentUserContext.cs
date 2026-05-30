namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Abstraction of the authenticated user so the Application layer does not
    /// depend on ASP.NET (HttpContext, ClaimsPrincipal).
    /// </summary>
    public interface ICurrentUserContext
    {
        /// <summary>Identity user id of the current user, or null when unauthenticated.</summary>
        string? UserId { get; }

        /// <summary>Remote IP address of the current request, or null when unavailable.</summary>
        string? IpAddress { get; }

        /// <summary>True when the current request is authenticated.</summary>
        bool IsAuthenticated { get; }

        /// <summary>Returns true when the current user belongs to the given role.</summary>
        /// <param name="role">Role name to check.</param>
        /// <returns>True if the user is in the role; otherwise false.</returns>
        bool IsInRole(string role);
    }
}
