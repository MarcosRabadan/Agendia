using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IEmployeeRepository
    {
        /// <summary>Gets a tracked employee by id, honouring the soft-delete and business-scope filters.</summary>
        /// <param name="id">Employee id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The employee, or null when soft-deleted, out of scope, or missing.</returns>
        Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a non-deleted employee by id for public (booking/availability) reads.
        /// Untracked; ignores the business-scope filter so it works regardless of the
        /// caller (the caller still checks business/active).
        /// </summary>
        /// <param name="id">Employee id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The employee, or null when soft-deleted or missing.</returns>
        Task<Employee?> GetByIdPublicAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets an employee by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Employee id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The employee even if soft-deleted, or null when missing.</returns>
        Task<Employee?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a business's active, non-deleted employees for the public availability flow,
        /// ordered by id. Untracked; ignores the business-scope filter.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The active employees of the business.</returns>
        Task<IEnumerable<Employee>> GetActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of employees ordered by id. Untracked.</summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of employees and the total count.</returns>
        Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of the employees belonging to the businesses owned by a user, ordered by id. Untracked.</summary>
        /// <param name="ownerUserId">Owner's identity user id.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of the owner's employees and the total count.</returns>
        Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByOwnerUserIdAsync(string ownerUserId,
                                                                                         int page,
                                                                                         int pageSize,
                                                                                         CancellationToken cancellationToken = default);

        /// <summary>Adds a new employee to the context.</summary>
        /// <param name="employee">The employee to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(Employee employee, CancellationToken cancellationToken = default);

        /// <summary>Marks an employee as modified.</summary>
        /// <param name="employee">The employee to update.</param>
        void Update(Employee employee);

        /// <summary>Removes an employee (soft-deleted by the save interceptor).</summary>
        /// <param name="employee">The employee to delete.</param>
        void Delete(Employee employee);
    }
}
