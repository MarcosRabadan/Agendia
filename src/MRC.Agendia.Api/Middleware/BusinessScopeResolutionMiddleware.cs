using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Api.Middleware
{
    /// <summary>
    /// Resolves the caller's business scope once per request, asynchronously, right after
    /// authentication (#313).
    ///
    /// <para><b>Why it needs its own middleware.</b> The scope is read from inside EF's global
    /// query filter (see <c>AgendiaDbContext</c>), and a query filter is evaluated
    /// synchronously. Resolving it lazily therefore meant a blocking database round-trip on a
    /// path that virtually every scoped request goes through, holding a thread pool thread for
    /// its duration. There is no way to await from inside the filter, so the lookup has to
    /// happen before the DbContext ever asks - which is here.</para>
    ///
    /// <para><b>Placement.</b> After <c>UseAuthentication</c>, because the scope reads the role
    /// and the subject off the authenticated principal; before the endpoints, because that is
    /// where the first scoped query happens.</para>
    ///
    /// <para><b>Cost.</b> None for anonymous, Admin and Client callers: the scope decides
    /// restriction from the role alone and returns without touching the database. Only
    /// Owner/Employee callers pay for the lookup, and they were paying for it anyway - just
    /// synchronously, and later.</para>
    /// </summary>
    public class BusinessScopeResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public BusinessScopeResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // ICurrentBusinessScope is scoped, so it is injected per request here rather than
        // through the constructor.
        public async Task InvokeAsync(HttpContext context, ICurrentBusinessScope businessScope)
        {
            await businessScope.EnsureResolvedAsync(context.RequestAborted);
            await _next(context);
        }
    }
}
