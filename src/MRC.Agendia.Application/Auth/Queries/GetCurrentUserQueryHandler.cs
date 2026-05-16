using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Queries
{
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
    {
        private readonly IAuthService _service;
        public GetCurrentUserQueryHandler(IAuthService service) { _service = service; }
        public Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
            => _service.GetCurrentUserAsync(request.UserId);
    }
}
