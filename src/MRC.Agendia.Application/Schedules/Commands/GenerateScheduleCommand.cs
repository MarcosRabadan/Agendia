using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public record GenerateScheduleCommand(GenerateScheduleRequestDto Dto) : IRequest<GenerateScheduleResponseDto>;
}
