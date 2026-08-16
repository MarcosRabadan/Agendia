using MediatR;
using MRC.Agendia.Application.Statistics.DTO;

namespace MRC.Agendia.Application.Statistics.Queries
{
    /// <summary>How well the business's agenda was used over a date range (inclusive).</summary>
    public record GetBusinessUtilizationQuery(Guid BusinessId, DateOnly From, DateOnly To)
        : IRequest<UtilizationDto>;
}
