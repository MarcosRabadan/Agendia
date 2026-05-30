using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Commands.Series
{
    public record MoveAppointmentSeriesCommand(Guid SeriesId, MoveAppointmentSeriesDto Dto) : IRequest<MoveAppointmentSeriesResultDto>;
}
