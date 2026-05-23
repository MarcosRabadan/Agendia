using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AgendiaDbContext _context;

        public UnitOfWork(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<int> Save(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
