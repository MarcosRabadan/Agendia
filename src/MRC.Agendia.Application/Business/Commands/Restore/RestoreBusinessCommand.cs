using MediatR;

namespace MRC.Agendia.Application.Business.Commands.Restore
{
    public record RestoreBusinessCommand(Guid Id) : IRequest<bool>;
}
