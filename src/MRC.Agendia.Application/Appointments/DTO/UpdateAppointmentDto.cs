using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record UpdateAppointmentDto(
        int Id,
        int ClientId,
        int EmployeeId,
        int ServiceId,
        DateTime StartDate,
        DateTime EndDate,
        AppointmentStatus Status,
        string? Notes);
}
