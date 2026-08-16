using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record AppointmentDto(
        Guid Id,
        string ClientUserId,
        Guid EmployeeId,
        Guid ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        AppointmentStatus Status,
        string? Notes,
        Guid? SeriesId = null,
        IReadOnlyList<Guid>? ExtraServiceIds = null,
        // Only filled when the operation cancelled the appointment through self-service
        // and the business has cancellation tiers (#270): tells the front what the
        // cancellation cost, so it can branch. Null everywhere else.
        AppliedCancellationTierDto? AppliedCancellationTier = null);
}
