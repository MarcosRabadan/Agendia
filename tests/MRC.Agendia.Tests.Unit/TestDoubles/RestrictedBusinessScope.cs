using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Tests.Unit.TestDoubles
{
    /// <summary>
    /// <see cref="ICurrentBusinessScope"/> test double restricted to a fixed set of businesses,
    /// as the real scope is for an Owner/Employee caller. Needed by the queries that drop the
    /// global filters and re-state the scope by hand (#292, #332): with an unrestricted double
    /// the cross-tenant half of that condition is never exercised.
    /// </summary>
    public sealed class RestrictedBusinessScope : ICurrentBusinessScope
    {
        private readonly Guid[] _businessIds;

        public RestrictedBusinessScope(params Guid[] businessIds) => _businessIds = businessIds;

        public bool IsRestricted => true;

        public IReadOnlyCollection<Guid> BusinessIds => _businessIds;

        // Nothing to resolve: this double never looks anything up (#313).
        public Task EnsureResolvedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
