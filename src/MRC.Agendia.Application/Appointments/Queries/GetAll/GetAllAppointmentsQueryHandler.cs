using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments.Queries.GetAll
{
    public class GetAllAppointmentsQueryHandler : IRequestHandler<GetAllAppointmentsQuery, PagedResult<AppointmentDto>>
    {
        private readonly IAppointmentService _service;

        public GetAllAppointmentsQueryHandler(IAppointmentService service)
        {
            _service = service;
        }

        public Task<PagedResult<AppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
