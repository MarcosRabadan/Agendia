using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto?>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public GetAppointmentByIdQueryHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageAppointmentAsync(request.Id, cancellationToken);
            return await _service.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
