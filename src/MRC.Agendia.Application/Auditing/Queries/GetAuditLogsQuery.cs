using MediatR;
using MRC.Agendia.Application.Auditing.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Auditing.Queries
{
    public record GetAuditLogsQuery(
        string? UserId,
        string? Action,
        string? EntityType,
        DateTime? From,
        DateTime? To,
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize) : IRequest<PagedResult<AuditLogDto>>;
}
