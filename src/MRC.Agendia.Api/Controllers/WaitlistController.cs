using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Application.Waitlist.Queries;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Waitlist.Commands.Join;
using MRC.Agendia.Application.Waitlist.Commands.Leave;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/waitlist")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.Client)]
    public class WaitlistController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WaitlistController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Adds the authenticated client to the waitlist for a full slot.
        /// If the slot still has room, returns 400 inviting them to book directly.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WaitlistEntryDto>> Join([FromBody] JoinWaitlistDto dto)
        {
            var result = await _mediator.Send(new JoinWaitlistCommand(dto));
            return Ok(result);
        }

        /// <summary>Lists the authenticated client's waitlist entries.</summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(IReadOnlyList<WaitlistEntryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<WaitlistEntryDto>>> GetMine()
        {
            var result = await _mediator.Send(new GetMyWaitlistQuery());
            return Ok(result);
        }

        /// <summary>Removes one of the client's own waitlist entries.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Leave(int id)
        {
            await _mediator.Send(new LeaveWaitlistCommand(id));
            return NoContent();
        }
    }
}
