using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands.Create
{
    public record CreateBusinessCommand(CreateBusinessDto Dto) : IRequest<BusinessDto>;
}
