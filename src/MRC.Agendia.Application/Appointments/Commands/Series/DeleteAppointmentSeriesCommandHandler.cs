using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands.Series
{
    public class DeleteAppointmentSeriesCommandHandler : IRequestHandler<DeleteAppointmentSeriesCommand, AppointmentSeriesCountResultDto>
    {
        private readonly IRecurringAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteAppointmentSeriesCommandHandler(IRecurringAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentSeriesCountResultDto> Handle(DeleteAppointmentSeriesCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageAppointmentSeriesAsync(request.SeriesId, cancellationToken);
            return await _service.DeleteSeriesAsync(request.SeriesId, cancellationToken);
        }
    }
}
