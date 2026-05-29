using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Waitlist.DTO;

namespace MRC.Agendia.Application.Waitlist.Queries
{
    public class GetMyWaitlistQueryHandler : IRequestHandler<GetMyWaitlistQuery, IReadOnlyList<WaitlistEntryDto>>
    {
        private readonly IWaitlistService _service;
        private readonly ICurrentUserContext _currentUser;

        public GetMyWaitlistQueryHandler(IWaitlistService service, ICurrentUserContext currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        public Task<IReadOnlyList<WaitlistEntryDto>> Handle(GetMyWaitlistQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No hay usuario autenticado.");

            return _service.GetMineAsync(userId, cancellationToken);
        }
    }
}
