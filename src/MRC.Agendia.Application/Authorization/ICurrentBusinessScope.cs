namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Per-request multi-tenant scope for the global business query filter (#58).
    /// When <see cref="IsRestricted"/> is true, reads of business-scoped entities
    /// return only rows whose business is in <see cref="BusinessIds"/>. This is
    /// defense in depth over <see cref="IResourceAuthorizationService"/>: it is
    /// bypassed (IsRestricted=false) for Admin, anonymous and Client callers; only
    /// Owner/Employee callers are restricted to their own business(es).
    /// </summary>
    public interface ICurrentBusinessScope
    {
        /// <summary>True only for Owner/Employee callers; false (no-op) otherwise.</summary>
        bool IsRestricted { get; }

        /// <summary>The business ids the caller may see (empty when not restricted).</summary>
        IReadOnlyCollection<Guid> BusinessIds { get; }

        /// <summary>
        /// Resolves the scope up front, asynchronously (#313).
        ///
        /// <para>The two properties above are read from inside EF's global query filter, and
        /// that filter is synchronous: resolving lazily means blocking a thread pool thread on
        /// a database round-trip, on a path that nearly every scoped request goes through.
        /// Calling this after authentication turns it into an ordinary await.</para>
        ///
        /// <para>Idempotent and cheap to call blindly: for anonymous, Admin and Client callers
        /// the scope is decided from the role alone and never touches the database.</para>
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the lookup.</param>
        Task EnsureResolvedAsync(CancellationToken cancellationToken = default);
    }
}
