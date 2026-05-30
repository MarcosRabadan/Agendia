using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands.Update
{
    public record UpdateServiceCommand(UpdateServiceDto Dto) : IRequest<ServiceDto>;
}
