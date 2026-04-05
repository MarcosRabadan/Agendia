using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public record GetAppointmentByIdQuery(int Id) : IRequest<AppointmentDto?>;
    public record GetAllAppointmentsQuery() : IRequest<IEnumerable<AppointmentDto>>;

    public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto?>
    {
        private readonly IAppointmentRepository _repository;

        public GetAppointmentByIdQueryHandler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<AppointmentDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new AppointmentDto(entity.Id, entity.ClientId, entity.EmployeeId, entity.ServiceId, entity.StartDate, entity.EndDate, entity.Status, entity.Notes);
        }
    }

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
