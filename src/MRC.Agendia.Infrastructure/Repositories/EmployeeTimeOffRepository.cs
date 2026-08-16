using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    /// <summary>
    /// Reads and writes the ad-hoc blocks on an employee's agenda. The overlap predicate
    /// (half-open [from, to): <c>Start &lt; to &amp;&amp; End &gt; from</c>) is spelled out in every
    /// query on purpose - a shared helper method would not translate to SQL.
    /// </summary>
    public class EmployeeTimeOffRepository : RepositoryBase<EmployeeTimeOff>, IEmployeeTimeOffRepository
    {
        public EmployeeTimeOffRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<EmployeeTimeOff>> GetByEmployeeAndRangeAsync(Guid employeeId,
                                                                                     DateTime from,
                                                                                     DateTime to,
                                                                                     CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(t => t.EmployeeId == employeeId && t.Start < to && t.End > from)
                .OrderBy(t => t.Start)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<EmployeeTimeOff>> GetByEmployeesAndRangeAsync(IReadOnlyCollection<Guid> employeeIds,
                                                                                      DateTime from,
                                                                                      DateTime to,
                                                                                      CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(t => employeeIds.Contains(t.EmployeeId) && t.Start < to && t.End > from)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public Task<bool> HasOverlapAsync(Guid employeeId,
                                          DateTime start,
                                          DateTime end,
                                          CancellationToken cancellationToken = default)
            => Set.AnyAsync(t => t.EmployeeId == employeeId && t.Start < end && t.End > start, cancellationToken);
    }
}
