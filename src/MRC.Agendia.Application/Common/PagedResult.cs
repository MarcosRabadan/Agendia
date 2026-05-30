namespace MRC.Agendia.Application.Common
{
    /// <summary>
    /// Standard envelope for paginated list responses.
    /// </summary>
    /// <typeparam name="T">DTO type of the items.</typeparam>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages)
    {
        public static PagedResult<T> Create(IReadOnlyList<T> items,
                                            int totalCount,
                                            int page,
                                            int pageSize)
        {
            var totalPages = pageSize <= 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);
            return new PagedResult<T>(items, page, pageSize, totalCount, totalPages);
        }
    }
}
