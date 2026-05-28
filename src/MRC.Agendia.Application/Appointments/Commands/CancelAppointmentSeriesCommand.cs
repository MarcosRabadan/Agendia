using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public record CancelAppointmentSeriesCommand(Guid SeriesId) : IRequest<AppointmentSeriesCountResultDto>;
}
