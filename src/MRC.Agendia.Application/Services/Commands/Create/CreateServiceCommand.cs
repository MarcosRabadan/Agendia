using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands.Create
{
    public record CreateServiceCommand(CreateServiceDto Dto) : IRequest<ServiceDto>;
}
