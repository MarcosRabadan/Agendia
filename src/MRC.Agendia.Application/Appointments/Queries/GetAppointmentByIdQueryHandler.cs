using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto?>
    {
        private readonly IAppointmentService _service;

        public GetAppointmentByIdQueryHandler(IAppointmentService service)
        {
            _service = service;
        }

        public async Task<AppointmentDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
