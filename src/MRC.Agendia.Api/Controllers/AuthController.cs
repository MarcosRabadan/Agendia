using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.ServiceAuth.Commands;
using MRC.Agendia.Application.ServiceAuth.DTO;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Machine-to-machine authentication (#232). Trusted services exchange their
    /// client credentials for a short-lived service token (no refresh token). User
    /// authentication lives in the Harmony identity service, not here; see
    /// docs/service-auth-contract.md.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Exchanges a clientId + clientSecret for a service access token.</summary>
        [AllowAnonymous]
        [HttpPost("service-token")]
        [ProducesResponseType(typeof(ServiceTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ServiceTokenResponseDto>> ServiceToken([FromBody] ServiceTokenRequestDto dto)
        {
            var response = await _mediator.Send(new AuthenticateServiceCommand(dto));
            return Ok(response);
        }
    }
}
