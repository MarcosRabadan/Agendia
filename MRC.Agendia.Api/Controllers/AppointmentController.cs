using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Appointments.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllAppointmentsQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetAppointmentByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentDto dto)
        {
            var result = await _mediator.Send(new CreateAppointmentCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentDto>> Update(int id, [FromBody] UpdateAppointmentDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateAppointmentCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteAppointmentCommand(id));
            return NoContent();
        }
    }
}
