using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Queries
{
    public record GetCurrentUserQuery(string UserId) : IRequest<UserDto>;
}
