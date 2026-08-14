using MediatR;

namespace MRC.Agendia.Application.Services.Commands.Delete
{
    public record DeleteServiceCommand(Guid Id) : IRequest<bool>;
}
