using MediatR;

namespace MRC.Agendia.Application.Services.Commands
{
    public record RestoreServiceCommand(int Id) : IRequest<bool>;
}
