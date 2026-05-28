using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CancelAppointmentSeriesCommandHandler : IRequestHandler<CancelAppointmentSeriesCommand, AppointmentSeriesCountResultDto>
    {
        private readonly IRecurringAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public CancelAppointmentSeriesCommandHandler(IRecurringAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentSeriesCountResultDto> Handle(CancelAppointmentSeriesCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageAppointmentSeriesAsync(request.SeriesId, cancellationToken);
            return await _service.CancelSeriesAsync(request.SeriesId, cancellationToken);
        }
    }
}
