using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class BusinessRepository : RepositoryBase<Business>, IBusinessRepository
    {
        public BusinessRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public Task<Business?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        // IgnoreQueryFilters so availability works regardless of the caller's business
        // scope (#58); re-apply !IsDeleted explicitly since the global filter is bypassed.
        /// <inheritdoc />
        public Task<Business?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && !b.IsDeleted, cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<CancellationPolicyTier>> GetCancellationTiersAsync(Guid businessId,
                                                                                           CancellationToken cancellationToken = default)
            => await Context.CancellationPolicyTiers
                .AsNoTracking()
                .Where(t => t.BusinessId == businessId)
                .OrderByDescending(t => t.MinHoursBefore)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task ReplaceCancellationTiersAsync(Guid businessId,
                                                        IReadOnlyList<CancellationPolicyTier> tiers,
                                                        CancellationToken cancellationToken = default)
        {
            var current = await Context.CancellationPolicyTiers
                .Where(t => t.BusinessId == businessId)
                .ToListAsync(cancellationToken);

            // Diffed by threshold instead of delete-then-insert: the whole replacement
            // lands in ONE save, and EF is free to order deletes after inserts, which
            // would trip the unique (business, threshold) index if a threshold that stays
            // were re-inserted as a new row.
            var incoming = tiers.ToDictionary(t => t.MinHoursBefore);

            foreach (var existing in current)
            {
                if (incoming.TryGetValue(existing.MinHoursBefore, out var replacement))
                {
                    existing.PenaltyKind = replacement.PenaltyKind;
                    existing.PenaltyValue = replacement.PenaltyValue;
                    incoming.Remove(existing.MinHoursBefore);
                }
                else
                {
                    Context.CancellationPolicyTiers.Remove(existing);
                }
            }

            foreach (var added in incoming.Values)
            {
                added.BusinessId = businessId;
                await Context.CancellationPolicyTiers.AddAsync(added, cancellationToken);
            }
        }
    }
}
