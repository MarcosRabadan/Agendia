using MediatR;
using MRC.Agendia.Application.Availability.DTO;

namespace MRC.Agendia.Application.Availability.Queries
{
    public record GetAvailabilityQuery(
        Guid BusinessId,
        DateOnly Date,
        Guid ServiceId,
        Guid? EmployeeId,
        int StepMinutes,
        IReadOnlyList<Guid>? ExtraServiceIds = null) : IRequest<AvailabilityDto>;
}
