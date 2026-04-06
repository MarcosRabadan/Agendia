using MediatR;

namespace MRC.Agendia.Application.Business.Commands
{
    public record DeleteBusinessCommand(int Id) : IRequest<bool>;
}
