using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record CreateAppointmentDto(
        string ClientUserId,
        Guid EmployeeId,
        Guid ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        string? Notes,
        IReadOnlyList<Guid>? ExtraServiceIds = null,
        AppointmentStatus? Status = null);
}
