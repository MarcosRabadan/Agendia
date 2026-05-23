namespace MRC.Agendia.Domain.Common
{
    /// <summary>
    /// Entities that track who created/updated them and when. The values are
    /// filled automatically by the persistence interceptor, never by callers.
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? CreatedBy { get; set; }
        string? UpdatedBy { get; set; }
    }
}
