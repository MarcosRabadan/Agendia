using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Queries
{
    public record GetAllServicesQuery() : IRequest<IEnumerable<ServiceDto>>;
}
