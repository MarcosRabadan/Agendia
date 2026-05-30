using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Auditing.DTO;
using MRC.Agendia.Application.Auditing.Queries;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = Roles.Admin)]
    [Produces("application/json")]
    public class AuditLogController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Paged audit-log listing with filters (Admin only).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(
                new GetAuditLogsQuery(userId, action, entityType, from, to, page, pageSize));
            return Ok(result);
        }
    }
}
