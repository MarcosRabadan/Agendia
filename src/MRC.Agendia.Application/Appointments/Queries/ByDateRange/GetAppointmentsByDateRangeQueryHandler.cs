using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Queries.ByDateRange
{
    public class GetAppointmentsByDateRangeQueryHandler : IRequestHandler<GetAppointmentsByDateRangeQuery, IEnumerable<AppointmentDto>>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public GetAppointmentsByDateRangeQueryHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<IEnumerable<AppointmentDto>> Handle(GetAppointmentsByDateRangeQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);
            return await _service.GetByBusinessIdAndDateRangeAsync(request.BusinessId, request.StartDate, request.EndDate, cancellationToken);
        }
    }
}
