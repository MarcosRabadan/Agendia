namespace MRC.Agendia.Application.Employees.DTO
{
    public record UpdateEmployeeDto(
        int Id,
        bool IsActive,
        int MaxConcurrentAppointments);
}
