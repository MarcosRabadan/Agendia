using MediatR;

namespace MRC.Agendia.Application.Clients.Commands.Delete
{
    public record DeleteClientCommand(int Id) : IRequest<bool>;
}
