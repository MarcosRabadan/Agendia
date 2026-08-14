using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record UpdateAppointmentDto(
        Guid Id,
        string ClientUserId,
        Guid EmployeeId,
        Guid ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        AppointmentStatus Status,
        string? Notes);
}
