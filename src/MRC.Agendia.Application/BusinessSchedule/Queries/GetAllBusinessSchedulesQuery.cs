using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
    public record GetAllBusinessSchedulesQuery() : IRequest<IEnumerable<BusinessScheduleDto>>;
}
