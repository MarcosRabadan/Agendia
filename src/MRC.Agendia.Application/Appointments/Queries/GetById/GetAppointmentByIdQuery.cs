using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Queries.GetById
{
    public record GetAppointmentByIdQuery(int Id) : IRequest<AppointmentDto?>;
}
