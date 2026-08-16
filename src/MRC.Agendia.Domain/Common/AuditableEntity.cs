namespace MRC.Agendia.Domain.Common
{
    /// <summary>
    /// Base class for entities that carry audit fields and support soft delete.
    /// The values are filled automatically by the persistence interceptor. Inherits the
    /// domain-events plumbing from <see cref="Entity"/>.
    /// </summary>
    public abstract class AuditableEntity : Entity, IAuditable, ISoftDelete
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
