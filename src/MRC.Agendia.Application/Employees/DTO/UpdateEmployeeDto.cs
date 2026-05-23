namespace MRC.Agendia.Application.Employees.DTO
{
    public record UpdateEmployeeDto(
        int Id,
        string FullName,
        string? Email,
        string? Phone,
        bool IsActive,
        int MaxConcurrentAppointments);
}
