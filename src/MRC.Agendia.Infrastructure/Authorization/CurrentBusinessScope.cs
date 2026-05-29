using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.Authorization
{
    /// <summary>
    /// Resolves the caller's business scope once per request (memoized). Owner and
    /// Employee callers are restricted to their own business id(s); Admin,
    /// anonymous and Client callers are unrestricted (the global filter is a no-op
    /// for them). The business-id lookup runs on a separate DI scope with filters
    /// off, because the request's own DbContext filter calls back into this service
    /// (querying that same context here would re-enter it).
    /// </summary>
    public class CurrentBusinessScope : ICurrentBusinessScope
    {
        private readonly ICurrentUserContext _currentUser;
        private readonly IServiceScopeFactory _scopeFactory;

        private bool _resolved;
        private bool _isRestricted;
        private int[] _businessIds = Array.Empty<int>();

        public CurrentBusinessScope(ICurrentUserContext currentUser, IServiceScopeFactory scopeFactory)
        {
            _currentUser = currentUser;
            _scopeFactory = scopeFactory;
        }

        public bool IsRestricted
        {
            get { Resolve(); return _isRestricted; }
        }

        public IReadOnlyCollection<int> BusinessIds
        {
            get { Resolve(); return _businessIds; }
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            // Unauthenticated and Admin callers see everything -> no restriction.
            if (!_currentUser.IsAuthenticated
                || string.IsNullOrEmpty(_currentUser.UserId)
                || _currentUser.IsInRole(Roles.Admin))
                return;

            var userId = _currentUser.UserId!;

            // Resolve on a separate scope/context with filters OFF: the request's
            // own DbContext filter calls into this service, so querying it here
            // would re-enter. IgnoreQueryFilters also bypasses the new business
            // filter, so this lookup is never recursive.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var ownerBusinessIds = db.Businesses
                .IgnoreQueryFilters()
                .Where(b => b.OwnerUserId == userId)
                .Select(b => b.Id);

            var employeeBusinessIds = db.Employees
                .IgnoreQueryFilters()
                .Where(e => e.UserId == userId && !e.IsDeleted)
                .Select(e => e.BusinessId);

            _businessIds = ownerBusinessIds.Union(employeeBusinessIds).ToArray();

            // Only Owner/Employee callers (who actually belong to a business) are
            // restricted; a Client has no business and stays unrestricted.
            _isRestricted = _businessIds.Length > 0;
        }
    }
}
