using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// Access to the ad-hoc blocks on an employee's agenda (#271). Ranges are wall-clock
    /// and half-open [Start, End), the same convention appointments use.
    /// </summary>
    public interface IEmployeeTimeOffRepository
    {
        /// <summary>Gets a tracked block by id, or null when missing.</summary>
        Task<EmployeeTimeOff?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>The blocks of one employee overlapping [from, to), oldest first.</summary>
        Task<IReadOnlyList<EmployeeTimeOff>> GetByEmployeeAndRangeAsync(Guid employeeId,
                                                                        DateTime from,
                                                                        DateTime to,
                                                                        CancellationToken cancellationToken = default);

        /// <summary>
        /// The blocks of several employees overlapping [from, to), for the availability
        /// calculation (one query for the whole day instead of one per employee).
        /// </summary>
        Task<IReadOnlyList<EmployeeTimeOff>> GetByEmployeesAndRangeAsync(IReadOnlyCollection<Guid> employeeIds,
                                                                         DateTime from,
                                                                         DateTime to,
                                                                         CancellationToken cancellationToken = default);

        /// <summary>True when the employee has any block overlapping [start, end).</summary>
        Task<bool> HasOverlapAsync(Guid employeeId,
                                   DateTime start,
                                   DateTime end,
                                   CancellationToken cancellationToken = default);

        /// <summary>Adds a new block to the context.</summary>
        Task AddAsync(EmployeeTimeOff timeOff, CancellationToken cancellationToken = default);

        /// <summary>Removes a block (hard delete: a block carries no history worth keeping).</summary>
        void Delete(EmployeeTimeOff timeOff);
    }
}
