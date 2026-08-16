using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Application.Statistics.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:guid}/stats")]
    [Produces("application/json")]
    public class BusinessStatsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessStatsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Business statistics over a date range: bookings per month and week,
        /// most and least used services (by count), no-shows and cancellations, and
        /// booking counts per hour and per weekday. Revenue is not reported: Agendia
        /// does not own the service price (it lives in the catalog service). Business
        /// staff only (owner, an active employee of the business, or an admin).
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpGet]
        [ProducesResponseType(typeof(BusinessStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BusinessStatsDto>> GetStats(
            Guid businessId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            var result = await _mediator.Send(new GetBusinessStatsQuery(businessId, from, to));
            return Ok(result);
        }

        /// <summary>
        /// How well the agenda was used over a date range: occupancy overall, per hour of
        /// the day, per weekday and per employee, plus how far in advance clients book on
        /// average. Measured in minutes of agenda (the effective schedule's open minutes
        /// times each employee's capacity, minus their time off), so no price is involved:
        /// the catalog service owns those. Range capped at 92 days. Business staff only
        /// (owner, an active employee of the business, or an admin).
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpGet("utilization")]
        [ProducesResponseType(typeof(UtilizationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UtilizationDto>> GetUtilization(
            Guid businessId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            var result = await _mediator.Send(new GetBusinessUtilizationQuery(businessId, from, to));
            return Ok(result);
        }
    }
}
