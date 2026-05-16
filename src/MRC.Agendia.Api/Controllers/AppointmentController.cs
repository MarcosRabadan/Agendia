using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Appointments.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AppointmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllAppointmentsQuery());
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetAppointmentByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentDto dto)
        {
            var result = await _mediator.Send(new CreateAppointmentCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentDto>> Update(int id, [FromBody] UpdateAppointmentDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateAppointmentCommand(dto));
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteAppointmentCommand(id));
            return NoContent();
        }
    }
}
