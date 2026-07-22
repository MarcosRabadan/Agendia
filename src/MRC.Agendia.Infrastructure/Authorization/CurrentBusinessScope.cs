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
    ///
    /// Restriction is decided by ROLE, not by whether the lookup found rows: an
    /// Owner/Employee with no rows is restricted to nothing rather than to
    /// everything. That distinction did not matter while Agendia minted its own
    /// accounts alongside the business row, but Harmony issues roles independently,
    /// so a token can now legitimately carry BusinessOwner before (or without) the
    /// matching row ever being provisioned here.
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

        /// <inheritdoc />
        public bool IsRestricted
        {
            get { Resolve(); return _isRestricted; }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<int> BusinessIds
        {
            get { Resolve(); return _businessIds; }
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            // Anonymous and Admin callers see everything -> no restriction.
            if (!_currentUser.IsAuthenticated || _currentUser.IsInRole(Roles.Admin))
                return;

            // Only tenant-bound roles are scoped. A Client browses the public
            // catalogue and belongs to no business, so it stays unrestricted.
            var isTenantBound = _currentUser.IsInRole(Roles.BusinessOwner)
                || _currentUser.IsInRole(Roles.Employee);
            if (!isTenantBound)
                return;

            // From here on the caller IS restricted, whatever the lookup returns.
            _isRestricted = true;

            // An authenticated token with no subject cannot be matched to any
            // business, so it gets the empty scope rather than a free pass.
            var userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return;

            // Resolve on a separate scope/context with filters OFF: the request's
            // own DbContext filter calls into this service, so querying it here
            // would re-enter. IgnoreQueryFilters also bypasses the new business
            // filter, so this lookup is never recursive.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var ownerBusinessIds = db.Businesses
                .IgnoreQueryFilters()
                .Where(b => b.OwnerUserId == userId && !b.IsDeleted)
                .Select(b => b.Id);

            var employeeBusinessIds = db.Employees
                .IgnoreQueryFilters()
                .Where(e => e.UserId == userId && !e.IsDeleted)
                .Select(e => e.BusinessId);

            _businessIds = ownerBusinessIds.Union(employeeBusinessIds).ToArray();
        }
    }
}
