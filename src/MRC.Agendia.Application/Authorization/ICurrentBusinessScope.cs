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
        IReadOnlyCollection<int> BusinessIds { get; }
    }
}
