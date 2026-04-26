using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Clients.Commands;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Clients.Queries;

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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllClientsQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetClientByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto dto)
        {
            var result = await _mediator.Send(new CreateClientCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

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

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteClientCommand(id));
            return NoContent();
        }
    }
}
