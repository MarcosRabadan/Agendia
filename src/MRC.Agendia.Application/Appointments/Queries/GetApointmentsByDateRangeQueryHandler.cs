using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetApointmentsByDateRangeQueryHandler : IRequestHandler<GetApointmentsByDateRangeQuery, IEnumerable<AppointmentDto>>
    {
        private readonly IAppointmentService _service;

        public GetApointmentsByDateRangeQueryHandler(IAppointmentService service)
        {
            _service = service;
        }

        public Task<IEnumerable<AppointmentDto>> Handle(GetApointmentsByDateRangeQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
