using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetAllAppointmentsQueryHandler : IRequestHandler<GetAllAppointmentsQuery, IEnumerable<AppointmentDto>>
    {
        private readonly IAppointmentRepository _repository;

        public GetAllAppointmentsQueryHandler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new AppointmentDto(e.Id, e.ClientId, e.EmployeeId, e.ServiceId, e.StartDate, e.EndDate, e.Status, e.Notes));
        }
    }
}
