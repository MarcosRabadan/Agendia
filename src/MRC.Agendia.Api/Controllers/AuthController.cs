using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MRC.Agendia.Api.Services;
using MRC.Agendia.Application.Auth.Commands;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Auth.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
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

        /// <summary>Registro publico de un cliente.</summary>
        [HttpPost("register/client")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> RegisterClient([FromBody] RegisterClientDto dto)
        {
            var result = await _mediator.Send(new RegisterClientCommand(dto));
            return Ok(result);
        }

        /// <summary>Autoregistro publico de un BusinessOwner + su Business + Employee asociado.</summary>
        [HttpPost("register/owner")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> RegisterOwner([FromBody] RegisterOwnerDto dto)
        {
            var result = await _mediator.Send(new RegisterOwnerCommand(dto));
            return Ok(result);
        }

        /// <summary>Solo Owner: crea un Employee + su user asociado a SU negocio.</summary>
        [HttpPost("register/employee")]
        [Authorize(Roles = Roles.BusinessOwner)]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> RegisterEmployee([FromBody] RegisterEmployeeDto dto)
        {
            var ownerUserId = User.GetUserId();
            var result = await _mediator.Send(new RegisterEmployeeCommand(dto, ownerUserId));
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var result = await _mediator.Send(new LoginCommand(dto));
            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(dto.RefreshToken));
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            await _mediator.Send(new LogoutCommand(dto.RefreshToken, User.GetUserId()));
            return NoContent();
        }

        /// <summary>Revoca todas las sesiones (refresh tokens) del usuario autenticado.</summary>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.GetUserId();
            await _mediator.Send(new LogoutAllCommand(userId));
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new GetCurrentUserQuery(userId));
            return Ok(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new ChangePasswordCommand(userId, dto));
            return NoContent();
        }

        /// <summary>Solicita un enlace de restablecimiento de contrasena. Responde siempre 204 (no revela si el email existe).</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-register")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _mediator.Send(new ForgotPasswordCommand(dto));
            return NoContent();
        }

        /// <summary>Restablece la contrasena con el token recibido por email.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _mediator.Send(new ResetPasswordCommand(dto));
            return NoContent();
        }

        /// <summary>Confirma el email de un usuario con el token enviado al registrarse.</summary>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            await _mediator.Send(new ConfirmEmailCommand(dto));
            return NoContent();
        }
    }
}
