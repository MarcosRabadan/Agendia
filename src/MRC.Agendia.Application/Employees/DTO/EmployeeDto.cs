namespace MRC.Agendia.Application.Employees.DTO
{
    // Scheduling projection of a bookable resource. Carries no profile data (name,
    // contact): the front resolves the display name from the management/identity
    // service using UserId when the resource has an account.
    public record EmployeeDto(
        int Id,
        int BusinessId,
        bool IsActive,
        int MaxConcurrentAppointments,
        string? UserId = null);
}
