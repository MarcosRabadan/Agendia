using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetMyAppointmentsAsClientQueryHandler
        : IRequestHandler<GetMyAppointmentsAsClientQuery, PagedResult<AppointmentDto>>
    {
        private readonly IAppointmentService _service;
        private readonly ICurrentUserContext _currentUser;

        public GetMyAppointmentsAsClientQueryHandler(
            IAppointmentService service,
            ICurrentUserContext currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        public Task<PagedResult<AppointmentDto>> Handle(GetMyAppointmentsAsClientQuery request, CancellationToken cancellationToken)
        {
            // Defense in depth: the controller already requires [Authorize(Roles = Client)],
            // but the handler must not trust that and fail loudly if the claim is missing.
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("No hay usuario autenticado.");
            }

            return _service.GetPagedByClientUserIdAsync(userId, request.Page, request.PageSize);
        }
    }
}
