using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.Repositories
{
    /// <summary>
    /// EF Core helpers to apply pagination consistently across all repositories.
    /// </summary>
    internal static class PaginationExtensions
    {
        /// <summary>
        /// Runs two queries: a Count and a Skip/Take. Returns a tuple ready to
        /// be wrapped in a <see cref="PagedResult{T}"/> by the service layer.
        /// </summary>
        public static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPagedListAsync<T>(
            this IQueryable<T> source,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Clamp inputs defensively in case validators were bypassed.
            page = page < 1 ? PaginationConstants.DefaultPage : page;
            pageSize = pageSize switch
            {
                < 1 => PaginationConstants.DefaultPageSize,
                > PaginationConstants.MaxPageSize => PaginationConstants.MaxPageSize,
                _ => pageSize
            };

            var totalCount = await source.CountAsync(cancellationToken);
            var items = await source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
