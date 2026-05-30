using MediatR;

namespace MRC.Agendia.Application.Services.Commands.Restore
{
    public record RestoreServiceCommand(int Id) : IRequest<bool>;
}
