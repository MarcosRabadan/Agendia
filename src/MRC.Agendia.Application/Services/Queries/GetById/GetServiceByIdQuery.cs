using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Queries.GetById
{
    public record GetServiceByIdQuery(int Id) : IRequest<ServiceDto?>;
}
