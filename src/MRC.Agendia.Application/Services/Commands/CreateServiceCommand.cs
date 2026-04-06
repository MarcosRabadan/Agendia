using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands
{
    public record CreateServiceCommand(CreateServiceDto Dto) : IRequest<ServiceDto>;
}
