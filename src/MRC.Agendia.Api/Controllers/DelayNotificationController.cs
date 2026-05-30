using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Appointments.Commands.Delay;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:int}/notify-delay")]
    [Produces("application/json")]
    public class DelayNotificationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DelayNotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Notifies the clients with an upcoming appointment in the same slot that the
        /// business is running late (by the given minutes). Scope: the whole business
        /// or a specific employee. Business staff only.
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost]
        [ProducesResponseType(typeof(DelayNotificationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DelayNotificationResultDto>> NotifyDelay(int businessId, [FromBody] NotifyDelayDto dto)
        {
            var result = await _mediator.Send(new NotifyDelayCommand(businessId, dto));
            return Ok(result);
        }
    }
}
