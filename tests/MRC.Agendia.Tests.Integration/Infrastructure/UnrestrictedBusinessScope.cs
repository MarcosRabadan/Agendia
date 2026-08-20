using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// <see cref="ICurrentBusinessScope"/> test double that never restricts, for
    /// tests that construct an AgendiaDbContext directly (#58).
    /// </summary>
    public sealed class UnrestrictedBusinessScope : ICurrentBusinessScope
    {
        public bool IsRestricted => false;
        public IReadOnlyCollection<Guid> BusinessIds => Array.Empty<Guid>();

        // Nothing to resolve: this double never looks anything up (#313).
        public Task EnsureResolvedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
