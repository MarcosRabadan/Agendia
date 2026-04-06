using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public record CreateBusinessScheduleCommand(CreateBusinessScheduleDto Dto) : IRequest<BusinessScheduleDto>;
}
