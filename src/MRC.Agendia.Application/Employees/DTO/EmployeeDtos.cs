namespace MRC.Agendia.Application.Employees.DTO
{
    public record EmployeeDto(int Id, int BusinessId, string FullName, string? Email, string? Phone, bool IsActive);
    public record CreateEmployeeDto(int BusinessId, string FullName, string? Email, string? Phone);
    public record UpdateEmployeeDto(int Id, int BusinessId, string FullName, string? Email, string? Phone, bool IsActive);
}
