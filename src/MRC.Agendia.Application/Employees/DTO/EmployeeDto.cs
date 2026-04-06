namespace MRC.Agendia.Application.Employees.DTO
{
    public record EmployeeDto(int Id, int BusinessId, string FullName, string? Email, string? Phone, bool IsActive);
}
