using MediatR;

namespace MRC.Agendia.Application.Services.Commands
{
    public record DeleteServiceCommand(int Id) : IRequest<bool>;
}
