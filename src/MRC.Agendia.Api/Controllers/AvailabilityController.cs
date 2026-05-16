using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Availability.Queries;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Exposes the bookable time slots for a given business + service + day.
    /// Public endpoint: prospective clients can browse availability without
    /// having to log in.
    /// </summary>
    [ApiController]
    [Route("api/businesses/{businessId:int}/availability")]
    [Produces("application/json")]
    public class AvailabilityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AvailabilityController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the available booking windows for the requested service
        /// on the given date, optionally filtered by an employee.
        /// </summary>
        /// <param name="businessId">Business that offers the service.</param>
        /// <param name="date">Day to query, in ISO format (yyyy-MM-dd).</param>
        /// <param name="serviceId">Service the client wants to book.</param>
        /// <param name="employeeId">Optional: limit to one employee.</param>
        /// <param name="stepMinutes">
        /// Granularity of candidate start times, between 5 and 120. Default 15.
        /// </param>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AvailabilityDto>> Get(
            int businessId,
            [FromQuery] DateOnly date,
            [FromQuery] int serviceId,
            [FromQuery] int? employeeId = null,
            [FromQuery] int stepMinutes = 15)
        {
            var result = await _mediator.Send(
                new GetAvailabilityQuery(businessId, date, serviceId, employeeId, stepMinutes));
            return Ok(result);
        }
    }
}
