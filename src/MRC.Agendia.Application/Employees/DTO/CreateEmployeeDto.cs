namespace MRC.Agendia.Application.Employees.DTO
{
    // UserId is the Harmony user id, and is optional: an employee may be a resource
    // with no login at all (a room, a chair, a stylist who does not use the app).
    // UpdateEmployeeDto deliberately omits it so an existing employee can never be
    // repointed to another user via a crafted DTO.
    public record CreateEmployeeDto(
        int BusinessId,
        string FullName,
        string? Email,
        string? Phone,
        string? UserId = null,
        int MaxConcurrentAppointments = 1);
}
