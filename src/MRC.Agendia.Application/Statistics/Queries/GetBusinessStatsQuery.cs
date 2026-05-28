using MediatR;
using MRC.Agendia.Application.Statistics.DTO;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public record GetBusinessStatsQuery(int BusinessId, DateOnly From, DateOnly To) : IRequest<BusinessStatsDto>;
}
