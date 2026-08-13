using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record AppointmentDto(
        int Id,
        string ClientUserId,
        int EmployeeId,
        int ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        AppointmentStatus Status,
        string? Notes,
        Guid? SeriesId = null,
        IReadOnlyList<int>? ExtraServiceIds = null);
}
