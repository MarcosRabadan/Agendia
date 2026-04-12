using MediatR;

namespace MRC.Agendia.Application.Clients.Commands
{
    public record DeleteClientCommand(int Id) : IRequest<bool>;
}
