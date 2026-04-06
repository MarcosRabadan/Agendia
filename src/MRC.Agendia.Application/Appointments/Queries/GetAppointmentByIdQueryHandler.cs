using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Queries
{
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
}
