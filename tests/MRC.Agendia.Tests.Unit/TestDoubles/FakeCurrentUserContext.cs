using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Tests.Unit.TestDoubles
{
    /// <summary>
    /// In-memory test double for <see cref="ICurrentUserContext"/>. Roles are
    /// stored case-insensitively to match how ASP.NET role checks behave.
    /// </summary>
    public sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public string? UserId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsAuthenticated { get; set; }

        public bool IsInRole(string role) => _roles.Contains(role);

        public FakeCurrentUserContext WithRole(string role)
        {
            _roles.Add(role);
            return this;
        }
    }
}
