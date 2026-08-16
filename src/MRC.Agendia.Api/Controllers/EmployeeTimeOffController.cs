using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.TimeOff.Commands;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Application.TimeOff.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/employees/{employeeId:guid}/time-off")]
    [Produces("application/json")]
    [Authorize(Roles = RolePolicies.Staff)]
    public class EmployeeTimeOffController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeTimeOffController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// The employee's ad-hoc blocks (sick leave, a doctor's appointment, training)
        /// overlapping the given date range. Business staff only.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<EmployeeTimeOffDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<EmployeeTimeOffDto>>> Get(
            Guid employeeId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            var result = await _mediator.Send(new GetEmployeeTimeOffQuery(employeeId, from, to));
            return Ok(result);
        }

        /// <summary>
        /// Blocks the employee's agenda for a wall-clock range: those slots disappear from
        /// THEIR availability (the rest of the staff is unaffected) and no appointment can
        /// be booked or moved onto them (400 EMPLOYEE_UNAVAILABLE). Appointments already
        /// booked inside the range are NOT cancelled - they are returned in
        /// <c>collidingAppointmentIds</c> so the staff can deal with them.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CreateEmployeeTimeOffResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CreateEmployeeTimeOffResultDto>> Create(
            Guid employeeId,
            [FromBody] CreateEmployeeTimeOffDto dto)
        {
            var result = await _mediator.Send(new CreateEmployeeTimeOffCommand(employeeId, dto));
            return CreatedAtAction(nameof(Get), new { employeeId }, result);
        }

        /// <summary>Removes a block, freeing those slots again.</summary>
        [HttpDelete("{timeOffId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid employeeId, Guid timeOffId)
        {
            await _mediator.Send(new DeleteEmployeeTimeOffCommand(employeeId, timeOffId));
            return NoContent();
        }
    }
}
