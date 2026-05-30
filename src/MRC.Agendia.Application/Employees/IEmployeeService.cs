using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees
{
    public interface IEmployeeService
    {
        /// <summary>Gets a paged list of employees.</summary>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A paged result of employee DTOs.</returns>
        Task<PagedResult<EmployeeDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a paged list of employees belonging to the business owned by the given user.</summary>
        /// <param name="ownerUserId">The identifier of the owning user.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A paged result of employee DTOs.</returns>
        Task<PagedResult<EmployeeDto>> GetPagedByOwnerUserIdAsync(
            string ownerUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Gets an employee by its identifier.</summary>
        /// <param name="id">The employee identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The employee DTO, or <c>null</c> if not found.</returns>
        Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a new employee.</summary>
        /// <param name="dto">The data used to create the employee.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created employee DTO.</returns>
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing employee.</summary>
        /// <param name="dto">The data used to update the employee, including its identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated employee DTO.</returns>
        Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes an employee by its identifier.</summary>
        /// <param name="id">The employee identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the employee is deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted employee by its identifier.</summary>
        /// <param name="id">The employee identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the employee is restored or was not deleted.</returns>
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
