using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.DeviceTokens.DTO;
using MRC.Agendia.Application.DeviceTokens.Commands.Register;
using MRC.Agendia.Application.DeviceTokens.Commands.Remove;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Registration and removal of device tokens for push notifications (#51).
    /// Each authenticated user manages only their own.
    /// </summary>
    [ApiController]
    [Route("api/notifications/device-tokens")]
    [Produces("application/json")]
    [Authorize]
    public class DeviceTokenController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DeviceTokenController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Registers (or reassigns to the current user) a device token for push.</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenDto dto)
        {
            await _mediator.Send(new RegisterDeviceTokenCommand(dto));
            return NoContent();
        }

        /// <summary>Removes a device token from the current user (idempotent).</summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove([FromBody] RemoveDeviceTokenDto dto)
        {
            await _mediator.Send(new RemoveDeviceTokenCommand(dto));
            return NoContent();
        }
    }
}
