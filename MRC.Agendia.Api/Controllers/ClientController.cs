using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Clients.Commands;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Clients.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClientController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllClientsQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetClientByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto dto)
        {
            var result = await _mediator.Send(new CreateClientCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ClientDto>> Update(int id, [FromBody] UpdateClientDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateClientCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteClientCommand(id));
            return NoContent();
        }
    }
}
