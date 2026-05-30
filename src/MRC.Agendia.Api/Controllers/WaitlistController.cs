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
        /// Apunta al cliente autenticado a la lista de espera de una franja completa.
        /// Si la franja todavia tiene hueco, devuelve 400 invitando a reservar directamente.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WaitlistEntryDto>> Join([FromBody] JoinWaitlistDto dto)
        {
            var result = await _mediator.Send(new JoinWaitlistCommand(dto));
            return Ok(result);
        }

        /// <summary>Lista las entradas de lista de espera del cliente autenticado.</summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(IReadOnlyList<WaitlistEntryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<WaitlistEntryDto>>> GetMine()
        {
            var result = await _mediator.Send(new GetMyWaitlistQuery());
            return Ok(result);
        }

        /// <summary>Da de baja una entrada propia de la lista de espera.</summary>
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
