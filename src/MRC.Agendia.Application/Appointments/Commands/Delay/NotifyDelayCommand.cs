using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Commands.Delay
{
    public record NotifyDelayCommand(int BusinessId, NotifyDelayDto Dto) : IRequest<DelayNotificationResultDto>;
}
