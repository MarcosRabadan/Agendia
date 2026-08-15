using MediatR;

namespace MRC.Agendia.Application.Services.Commands.Restore
{
    public record RestoreServiceCommand(Guid Id) : IRequest<bool>;
}
