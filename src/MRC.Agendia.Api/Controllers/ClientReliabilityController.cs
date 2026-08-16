using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Application.Statistics.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:guid}/clients/{clientUserId}/reliability")]
    [Produces("application/json")]
    public class ClientReliabilityController : ControllerBase
    {
        private const int DefaultWindowDays = 90;

        private readonly IMediator _mediator;

        public ClientReliabilityController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Attendance record of a client in this business over the last <paramref name="days"/>
        /// days (default 90): how many appointments they had, how many they completed, missed
        /// (no-show) or cancelled, plus the no-show and cancellation rates. Only elapsed
        /// appointments count, and only this business's own appointments - a business never
        /// sees the client's history elsewhere. Metrics only: the client's name and contact
        /// belong to the identity/management services, not to Agendia. Business staff only
        /// (owner, an active employee of the business, or an admin).
        /// </summary>
        /// <param name="businessId">Business the appointments belong to.</param>
        /// <param name="clientUserId">The client's Harmony user id ("sub").</param>
        /// <param name="days">Length of the window in days (1-366, default 90).</param>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpGet]
        [ProducesResponseType(typeof(ClientReliabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ClientReliabilityDto>> GetReliability(
            Guid businessId,
            string clientUserId,
            [FromQuery] int days = DefaultWindowDays)
        {
            var result = await _mediator.Send(new GetClientReliabilityQuery(businessId, clientUserId, days));
            return Ok(result);
        }
    }
}
