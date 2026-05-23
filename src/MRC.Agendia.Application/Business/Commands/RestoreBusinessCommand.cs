using MediatR;

namespace MRC.Agendia.Application.Business.Commands
{
    public record RestoreBusinessCommand(int Id) : IRequest<bool>;
}
