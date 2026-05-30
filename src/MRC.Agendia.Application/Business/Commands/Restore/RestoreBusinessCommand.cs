using MediatR;

namespace MRC.Agendia.Application.Business.Commands.Restore
{
    public record RestoreBusinessCommand(int Id) : IRequest<bool>;
}
