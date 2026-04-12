using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public record GetAppointmentByIdQuery(int Id) : IRequest<AppointmentDto?>;
}
