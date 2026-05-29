using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Tests.Unit.TestDoubles
{
    /// <summary>
    /// <see cref="ICurrentBusinessScope"/> test double that never restricts, so a
    /// directly-constructed DbContext behaves as it did before the #58 filter.
    /// </summary>
    public sealed class UnrestrictedBusinessScope : ICurrentBusinessScope
    {
        public bool IsRestricted => false;
        public IReadOnlyCollection<int> BusinessIds => Array.Empty<int>();
    }
}
