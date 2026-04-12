namespace MRC.Agendia.Application.Employees.DTO
{
    public record CreateEmployeeDto(int BusinessId, string FullName, string? Email, string? Phone);
}
