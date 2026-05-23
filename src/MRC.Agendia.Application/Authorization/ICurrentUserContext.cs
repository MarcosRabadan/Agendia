namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Abstraction of the authenticated user so the Application layer does not
    /// depend on ASP.NET (HttpContext, ClaimsPrincipal).
    /// </summary>
    public interface ICurrentUserContext
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
}
