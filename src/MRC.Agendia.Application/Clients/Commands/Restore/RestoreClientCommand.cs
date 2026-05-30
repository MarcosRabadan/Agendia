using MediatR;

namespace MRC.Agendia.Application.Clients.Commands.Restore
{
    public record RestoreClientCommand(int Id) : IRequest<bool>;
}
