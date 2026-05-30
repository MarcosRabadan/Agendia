using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands.Update
{
    public record UpdateBusinessCommand(UpdateBusinessDto Dto) : IRequest<BusinessDto>;
}
