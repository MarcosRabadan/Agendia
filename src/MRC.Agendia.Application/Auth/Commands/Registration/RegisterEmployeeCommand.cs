using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.Registration
{
    public record RegisterEmployeeCommand(RegisterEmployeeDto Dto, string CurrentOwnerUserId) : IRequest<UserDto>;
}
