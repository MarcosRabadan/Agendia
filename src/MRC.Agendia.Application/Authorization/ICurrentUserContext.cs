namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Abstraccion del usuario autenticado para que la capa Application
    /// no dependa de ASP.NET (HttpContext, ClaimsPrincipal).
    /// </summary>
    public interface ICurrentUserContext
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
    }
}
