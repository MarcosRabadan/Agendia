using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Waitlist.DTO;

namespace MRC.Agendia.Application.Waitlist.Commands
{
    public class JoinWaitlistCommandHandler : IRequestHandler<JoinWaitlistCommand, WaitlistEntryDto>
    {
        private readonly IWaitlistService _service;
        private readonly ICurrentUserContext _currentUser;

        public JoinWaitlistCommandHandler(IWaitlistService service, ICurrentUserContext currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        public Task<WaitlistEntryDto> Handle(JoinWaitlistCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No hay usuario autenticado.");

            return _service.JoinAsync(request.Dto, userId, cancellationToken);
        }
    }
}
