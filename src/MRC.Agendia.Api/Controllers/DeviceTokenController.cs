using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.DeviceTokens.DTO;
using MRC.Agendia.Application.DeviceTokens.Commands.Register;
using MRC.Agendia.Application.DeviceTokens.Commands.Remove;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Registro y baja de tokens de dispositivo para notificaciones push (#51).
    /// Cada usuario autenticado gestiona unicamente los suyos.
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

        /// <summary>Registra (o reasigna al usuario actual) un token de dispositivo para push.</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenDto dto)
        {
            await _mediator.Send(new RegisterDeviceTokenCommand(dto));
            return NoContent();
        }

        /// <summary>Da de baja un token de dispositivo del usuario actual (idempotente).</summary>
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
