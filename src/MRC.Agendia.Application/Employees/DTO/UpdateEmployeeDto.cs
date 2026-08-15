namespace MRC.Agendia.Application.Employees.DTO
{
    public record UpdateEmployeeDto(
        Guid Id,
        bool IsActive,
        int MaxConcurrentAppointments);
}
