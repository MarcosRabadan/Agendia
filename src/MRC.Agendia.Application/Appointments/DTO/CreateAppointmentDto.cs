using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record CreateAppointmentDto(
        int ClientId,
        int EmployeeId,
        int ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        string? Notes,
        IReadOnlyList<int>? ExtraServiceIds = null,
        AppointmentStatus? Status = null);
}
