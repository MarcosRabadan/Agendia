using MediatR;
using MRC.Agendia.Application.Auditing.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Auditing.Queries
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogsQueryHandler(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _repository.GetPagedFilteredAsync(
                request.UserId, request.Action, request.EntityType,
                request.From, request.To, request.Page, request.PageSize, cancellationToken);

            var dtos = items
                .Select(a => new AuditLogDto(
                    a.Id, a.Action, a.UserId, a.EntityType, a.EntityId, a.Details, a.Timestamp, a.IpAddress))
                .ToList();

            return PagedResult<AuditLogDto>.Create(dtos, totalCount, request.Page, request.PageSize);
        }
    }
}
