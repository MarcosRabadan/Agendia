using Microsoft.EntityFrameworkCore;

namespace MRC.Agendia.Infrastructure.Repositories
{
    /// <summary>
    /// Shared CRUD plumbing for the EF Core repositories. Concrete repositories
    /// inherit it and add their entity-specific queries. The semantics match what
    /// the repositories did before: FindAsync by key, plain ToList, and tracked
    /// Add/Update/Remove.
    /// </summary>
    public abstract class RepositoryBase<T> where T : class
    {
        protected readonly AgendiaDbContext Context;

        protected RepositoryBase(AgendiaDbContext context)
        {
            Context = context;
        }

        protected DbSet<T> Set => Context.Set<T>();

        /// <inheritdoc />
        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await Set.FindAsync(new object?[] { id }, cancellationToken);

        /// <inheritdoc />
        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
            => await Set.AddAsync(entity, cancellationToken);

        /// <inheritdoc />
        public virtual void Update(T entity)
            => Set.Update(entity);

        /// <inheritdoc />
        public virtual void Delete(T entity)
            => Set.Remove(entity);
    }
}
