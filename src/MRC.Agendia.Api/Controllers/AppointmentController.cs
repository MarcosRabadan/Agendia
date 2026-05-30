using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Appointments.Commands.Crud;
using MRC.Agendia.Application.Appointments.Commands.Series;
using MRC.Agendia.Application.Appointments.Queries.ByDateRange;
using MRC.Agendia.Application.Appointments.Queries.GetAll;
using MRC.Agendia.Application.Appointments.Queries.GetById;

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

        /// <summary>Returns a paged list of all appointments. Admin only.</summary>
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

        /// <summary>Gets a single appointment by id.</summary>
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

        /// <summary>Returns the appointments of a business that overlap the given date range. Staff only.</summary>
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

        /// <summary>Creates a new appointment.</summary>
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
        /// Updates an appointment (reschedule, change status or notes). A client can
        /// only cancel or reschedule their own appointment; if it is already within
        /// the business's advance-notice window it returns 400 CANCELLATION_WINDOW_ELAPSED.
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
        /// Deletes an appointment. For a client, if the appointment is already within
        /// the business's advance-notice window it returns 400 CANCELLATION_WINDOW_ELAPSED.
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
        /// Bulk-creates a recurring appointment series (e.g. every Friday at 16h until
        /// a date). Returns the created appointments and the skipped ones (with their
        /// reason) when an occurrence falls on a closed day, conflicts or is full.
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

        /// <summary>Cancels the future, active appointments of a recurring series.</summary>
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
        /// Reschedules (shifts) the future appointments of a series: new time and/or
        /// day shift. Occurrences that conflict are skipped and reported.
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

        /// <summary>Soft-deletes every appointment of a recurring series.</summary>
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
        /// Restores a previously soft-deleted appointment. Admin only.
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
