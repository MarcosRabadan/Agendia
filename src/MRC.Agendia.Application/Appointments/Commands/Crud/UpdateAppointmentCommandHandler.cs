using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateAppointmentCommandHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
        {
            // Authorize the existing appointment AND the destination: the update can
            // reassign ClientId/EmployeeId/ServiceId, so re-check the caller may book
            // for that client + employee. Stops a client spoofing another ClientId or
            // moving the appointment into another tenant's agenda.
            await _auth.EnsureCanManageAppointmentAsync(request.Dto.Id, cancellationToken);
            await _auth.EnsureCanCreateAppointmentAsync(request.Dto.ClientId, request.Dto.EmployeeId, cancellationToken);
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
