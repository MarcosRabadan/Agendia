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
        private Guid[] _businessIds = Array.Empty<Guid>();

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
        public IReadOnlyCollection<Guid> BusinessIds
        {
            get { Resolve(); return _businessIds; }
        }

        /// <inheritdoc />
        public async Task EnsureResolvedAsync(CancellationToken cancellationToken = default)
        {
            if (_resolved) return;
            _resolved = true;

            if (!NeedsLookup(out var userId))
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            _businessIds = await BusinessIdsOf(db, userId).ToArrayAsync(cancellationToken);
        }

        /// <summary>
        /// Lazy fallback for anything that reaches the DbContext without going through the
        /// pipeline: the background jobs (anonymous, so they never get as far as the query)
        /// and tests that build a context by hand. On a request the middleware has already
        /// resolved, so this is a field read (#313).
        /// </summary>
        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            if (!NeedsLookup(out var userId))
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            _businessIds = BusinessIdsOf(db, userId).ToArray();
        }

        /// <summary>
        /// Decides restriction from the ROLE and reports whether a database lookup is still
        /// needed. Shared by both paths so they cannot drift apart on who is restricted.
        /// </summary>
        /// <param name="userId">The caller's subject, when a lookup is needed.</param>
        /// <returns>True when the business ids still have to be read from the database.</returns>
        private bool NeedsLookup(out string userId)
        {
            userId = string.Empty;

            // Anonymous and Admin callers see everything -> no restriction.
            if (!_currentUser.IsAuthenticated || _currentUser.IsInRole(Roles.Admin))
                return false;

            // Only tenant-bound roles are scoped. A Client browses the public
            // catalogue and belongs to no business, so it stays unrestricted.
            var isTenantBound = _currentUser.IsInRole(Roles.BusinessOwner)
                || _currentUser.IsInRole(Roles.Employee);
            if (!isTenantBound)
                return false;

            // From here on the caller IS restricted, whatever the lookup returns.
            _isRestricted = true;

            // An authenticated token with no subject cannot be matched to any
            // business, so it gets the empty scope rather than a free pass.
            var subject = _currentUser.UserId;
            if (string.IsNullOrEmpty(subject))
                return false;

            userId = subject;
            return true;
        }

        /// <summary>
        /// The businesses the caller owns or works at, on a separate scope/context with
        /// filters OFF: the request's own DbContext filter calls into this service, so
        /// querying it here would re-enter. IgnoreQueryFilters also bypasses soft delete,
        /// hence the explicit !IsDeleted on both sides.
        /// </summary>
        private static IQueryable<Guid> BusinessIdsOf(AgendiaDbContext db, string userId)
        {
            var ownerBusinessIds = db.Businesses
                .IgnoreQueryFilters()
                .Where(b => b.OwnerUserId == userId && !b.IsDeleted)
                .Select(b => b.Id);

            var employeeBusinessIds = db.Employees
                .IgnoreQueryFilters()
                .Where(e => e.UserId == userId && !e.IsDeleted)
                .Select(e => e.BusinessId);

            return ownerBusinessIds.Union(employeeBusinessIds);
        }
    }
}
