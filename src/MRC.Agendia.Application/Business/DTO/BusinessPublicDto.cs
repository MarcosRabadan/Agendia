namespace MRC.Agendia.Application.Business.DTO
{
    /// <summary>
    /// Customer-facing projection of <see cref="Domain.Entities.Business"/>.
    /// Used by anonymous endpoints (GET /api/business and GET /api/business/{id}).
    /// Deliberately omits <c>Email</c> and <c>IsActive</c>:
    /// <list type="bullet">
    ///   <item><description>Email may be the owner's personal address - not safe to publish.</description></item>
    ///   <item><description>IsActive is an internal flag; inactive businesses are filtered out upstream instead of exposed.</description></item>
    /// </list>
    /// </summary>
    public record BusinessPublicDto(
        int Id,
        string Name,
        string? Description,
        string Address,
        string Phone,
        int? CancellationWindowHours = null);
}
