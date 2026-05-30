using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Appointments.Queries.MyAppointments;
using MRC.Agendia.Application.Clients.Commands.Create;
using MRC.Agendia.Application.Clients.Commands.Delete;
using MRC.Agendia.Application.Clients.Commands.Restore;
using MRC.Agendia.Application.Clients.Commands.Update;
using MRC.Agendia.Application.Clients.Queries.GetAll;
using MRC.Agendia.Application.Clients.Queries.GetById;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClientController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClientController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Gets a paged list of clients.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ClientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<ClientDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllClientsQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>Gets a client by its identifier.</summary>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetClientByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Gets the authenticated client's appointments, paged and ordered from
        /// most recent to oldest. The client identity is resolved from the JWT;
        /// no clientId is accepted in the URL.
        /// </summary>
        [Authorize(Roles = Roles.Client)]
        [HttpGet("me/appointments")]
        [ProducesResponseType(typeof(PagedResult<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<AppointmentDto>>> GetMyAppointments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetMyAppointmentsAsClientQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>Creates a new client.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto dto)
        {
            var result = await _mediator.Send(new CreateClientCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an existing client.</summary>
        [Authorize(Roles = RolePolicies.AdminOrSelfClient)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> Update(int id, [FromBody] UpdateClientDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateClientCommand(dto));
            return Ok(result);
        }

        /// <summary>Soft-deletes a client by its identifier.</summary>
        [Authorize(Roles = RolePolicies.AdminOrSelfClient)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteClientCommand(id));
            return NoContent();
        }

        /// <summary>Restores a previously soft-deleted client. Admin only.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreClientCommand(id));
            return NoContent();
        }
    }
}
