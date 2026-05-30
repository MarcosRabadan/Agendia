using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Appointments.Queries;
using MRC.Agendia.Application.Common;
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
        [ProducesResponseType(typeof(PagedResult<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<AppointmentDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllAppointmentsQuery(page, pageSize));
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

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpGet("business/{businessId:int}")]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByBusinessAndDateRange(
            int businessId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var result = await _mediator.Send(new GetAppointmentsByDateRangeQuery(businessId, startDate, endDate));
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

        /// <summary>
        /// Actualiza una cita (reprogramar, cambiar estado o notas). Un cliente solo
        /// puede cancelar o reprogramar su propia cita; si esta ya esta dentro de la
        /// ventana de antelacion del negocio devuelve 400 CANCELLATION_WINDOW_ELAPSED.
        /// </summary>
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

        /// <summary>
        /// Elimina una cita. Para un cliente, si la cita ya esta dentro de la ventana
        /// de antelacion del negocio devuelve 400 CANCELLATION_WINDOW_ELAPSED.
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteAppointmentCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Crea en masa una serie de citas recurrentes (p. ej. todos los viernes a
        /// las 16h hasta una fecha). Devuelve las citas creadas y las omitidas (con
        /// su motivo) cuando una ocurrencia cae en dia cerrado, choca o esta llena.
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("series")]
        [ProducesResponseType(typeof(AppointmentSeriesResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentSeriesResultDto>> CreateSeries([FromBody] CreateAppointmentSeriesDto dto)
        {
            var result = await _mediator.Send(new CreateAppointmentSeriesCommand(dto));
            return Ok(result);
        }

        /// <summary>Cancela las citas futuras y activas de una serie recurrente.</summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("series/{seriesId:guid}/cancel")]
        [ProducesResponseType(typeof(AppointmentSeriesCountResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentSeriesCountResultDto>> CancelSeries(Guid seriesId)
        {
            var result = await _mediator.Send(new CancelAppointmentSeriesCommand(seriesId));
            return Ok(result);
        }

        /// <summary>
        /// Reprograma (desplaza) las citas futuras de una serie: nueva hora y/o
        /// desplazamiento de dias. Las ocurrencias que choquen se omiten y se informan.
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("series/{seriesId:guid}/move")]
        [ProducesResponseType(typeof(MoveAppointmentSeriesResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MoveAppointmentSeriesResultDto>> MoveSeries(Guid seriesId, [FromBody] MoveAppointmentSeriesDto dto)
        {
            var result = await _mediator.Send(new MoveAppointmentSeriesCommand(seriesId, dto));
            return Ok(result);
        }

        /// <summary>Elimina (soft delete) todas las citas de una serie recurrente.</summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpDelete("series/{seriesId:guid}")]
        [ProducesResponseType(typeof(AppointmentSeriesCountResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AppointmentSeriesCountResultDto>> DeleteSeries(Guid seriesId)
        {
            var result = await _mediator.Send(new DeleteAppointmentSeriesCommand(seriesId));
            return Ok(result);
        }

        /// <summary>
        /// Restaura una cita previamente eliminada (soft delete). Solo Admin.
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreAppointmentCommand(id));
            return NoContent();
        }
    }
}
