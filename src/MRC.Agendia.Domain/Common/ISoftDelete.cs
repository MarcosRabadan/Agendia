namespace MRC.Agendia.Domain.Common
{
    /// <summary>
    /// Entities that are never physically removed: a delete flips
    /// <see cref="IsDeleted"/> and they are hidden by a global query filter.
    /// </summary>
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}
