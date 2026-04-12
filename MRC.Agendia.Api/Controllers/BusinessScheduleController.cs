using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.BusinessSchedule.Commands;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Application.BusinessSchedule.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BusinessScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessScheduleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BusinessScheduleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BusinessScheduleDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllBusinessSchedulesQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BusinessScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessScheduleDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetBusinessScheduleByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BusinessScheduleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BusinessScheduleDto>> Create([FromBody] CreateBusinessScheduleDto dto)
        {
            var result = await _mediator.Send(new CreateBusinessScheduleCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BusinessScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessScheduleDto>> Update(int id, [FromBody] UpdateBusinessScheduleDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateBusinessScheduleCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteBusinessScheduleCommand(id));
            return NoContent();
        }
    }
}
