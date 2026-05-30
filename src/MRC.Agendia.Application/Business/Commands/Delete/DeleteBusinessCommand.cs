using MediatR;

namespace MRC.Agendia.Application.Business.Commands.Delete
{
    public record DeleteBusinessCommand(int Id) : IRequest<bool>;
}
