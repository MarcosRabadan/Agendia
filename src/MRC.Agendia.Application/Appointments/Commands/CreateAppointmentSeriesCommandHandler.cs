using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CreateAppointmentSeriesCommandHandler : IRequestHandler<CreateAppointmentSeriesCommand, AppointmentSeriesResultDto>
    {
        private readonly IRecurringAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateAppointmentSeriesCommandHandler(IRecurringAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentSeriesResultDto> Handle(CreateAppointmentSeriesCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanCreateAppointmentAsync(request.Dto.ClientId, request.Dto.EmployeeId, cancellationToken);
            return await _service.CreateSeriesAsync(request.Dto, cancellationToken);
        }
    }
}
