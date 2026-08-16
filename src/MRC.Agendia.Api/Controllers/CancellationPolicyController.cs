using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Business.Commands.CancellationPolicy;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Business.Queries.CancellationPolicy;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:guid}/cancellation-policy")]
    [Produces("application/json")]
    public class CancellationPolicyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CancellationPolicyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// The business's cancellation tiers, from the most notice to the least: how far
        /// ahead a client must cancel and what each step costs them. An empty list means
        /// the business uses its single <c>CancellationWindowHours</c> threshold instead.
        /// Agendia never charges the penalty: it states the rule (the money lives in the
        /// payments/management service).
        /// </summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<CancellationPolicyTierDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CancellationPolicyTierDto>>> Get(Guid businessId)
        {
            var result = await _mediator.Send(new GetCancellationPolicyQuery(businessId));
            return Ok(result);
        }

        /// <summary>
        /// Replaces the whole cancellation policy of the business. The tiers must include
        /// one with 0 hours (so every cancellation falls in exactly one) and cannot repeat
        /// a threshold. Sending an empty list clears them and falls back to
        /// <c>CancellationWindowHours</c>. Owner of the business or admin.
        /// </summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPut]
        [ProducesResponseType(typeof(IReadOnlyList<CancellationPolicyTierDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<CancellationPolicyTierDto>>> Update(
            Guid businessId,
            [FromBody] UpdateCancellationPolicyDto dto)
        {
            var result = await _mediator.Send(new UpdateCancellationPolicyCommand(businessId, dto));
            return Ok(result);
        }
    }
}
