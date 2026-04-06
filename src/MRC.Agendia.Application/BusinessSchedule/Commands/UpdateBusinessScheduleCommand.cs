using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public record UpdateBusinessScheduleCommand(UpdateBusinessScheduleDto Dto) : IRequest<BusinessScheduleDto>;
}
