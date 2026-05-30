using MediatR;

namespace MRC.Agendia.Application.Services.Commands.Delete
{
    public record DeleteServiceCommand(int Id) : IRequest<bool>;
}
