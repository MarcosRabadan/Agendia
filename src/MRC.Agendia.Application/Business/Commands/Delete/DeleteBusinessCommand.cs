using MediatR;

namespace MRC.Agendia.Application.Business.Commands.Delete
{
    public record DeleteBusinessCommand(Guid Id) : IRequest<bool>;
}
