using MediatR;
using MRC.Agendia.Application.Statistics.DTO;

namespace MRC.Agendia.Application.Statistics.Queries
{
    /// <summary>Attendance record of a client in a business over the last <c>Days</c> days.</summary>
    public record GetClientReliabilityQuery(Guid BusinessId, string ClientUserId, int Days)
        : IRequest<ClientReliabilityDto>;
}
