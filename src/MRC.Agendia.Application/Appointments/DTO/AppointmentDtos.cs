using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    public record AppointmentDto(int Id, int ClientId, int EmployeeId, int ServiceId, DateTime StartDate, DateTime EndDate, AppointmentStatus Status, string? Notes);
    public record CreateAppointmentDto(int ClientId, int EmployeeId, int ServiceId, DateTime StartDate, DateTime EndDate, string? Notes);
    public record UpdateAppointmentDto(int Id, int ClientId, int EmployeeId, int ServiceId, DateTime StartDate, DateTime EndDate, AppointmentStatus Status, string? Notes);
}
