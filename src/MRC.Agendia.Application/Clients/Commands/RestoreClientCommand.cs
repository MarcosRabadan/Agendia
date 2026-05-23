using MediatR;

namespace MRC.Agendia.Application.Clients.Commands
{
    public record RestoreClientCommand(int Id) : IRequest<bool>;
}
