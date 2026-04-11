using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Services.Commands;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Application.Services.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ServiceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllServicesQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetServiceByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceDto dto)
        {
            var result = await _mediator.Send(new CreateServiceCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] UpdateServiceDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateServiceCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteServiceCommand(id));
            return NoContent();
        }
    }
}
