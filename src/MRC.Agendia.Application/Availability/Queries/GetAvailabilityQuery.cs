using MediatR;
using MRC.Agendia.Application.Availability.DTO;

namespace MRC.Agendia.Application.Availability.Queries
{
    public record GetAvailabilityQuery(
        int BusinessId,
        DateOnly Date,
        int ServiceId,
        int? EmployeeId,
        int StepMinutes,
        IReadOnlyList<int>? ExtraServiceIds = null) : IRequest<AvailabilityDto>;
}
