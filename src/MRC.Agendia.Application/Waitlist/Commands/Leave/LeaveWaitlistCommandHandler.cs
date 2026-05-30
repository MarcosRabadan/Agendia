using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Waitlist.Commands.Leave
{
    public class LeaveWaitlistCommandHandler : IRequestHandler<LeaveWaitlistCommand, bool>
    {
        private readonly IWaitlistService _service;
        private readonly ICurrentUserContext _currentUser;

        public LeaveWaitlistCommandHandler(IWaitlistService service, ICurrentUserContext currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(LeaveWaitlistCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No hay usuario autenticado.");

            await _service.LeaveAsync(request.EntryId, userId, cancellationToken);
            return true;
        }
    }
}
