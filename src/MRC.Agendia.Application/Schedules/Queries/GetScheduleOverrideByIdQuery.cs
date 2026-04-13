using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public record GetScheduleOverrideByIdQuery(int Id) : IRequest<ScheduleOverrideDto?>;
}
