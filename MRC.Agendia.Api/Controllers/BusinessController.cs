using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Business.Commands;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Business.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BusinessController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BusinessDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BusinessDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllBusinessesQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetBusinessByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BusinessDto>> Create([FromBody] CreateBusinessDto dto)
        {
            var result = await _mediator.Send(new CreateBusinessCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessDto>> Update(int id, [FromBody] UpdateBusinessDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateBusinessCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteBusinessCommand(id));
            return NoContent();
        }
    }
}
