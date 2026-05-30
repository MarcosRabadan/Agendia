using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MRC.Agendia.Api.Services;
using MRC.Agendia.Api.Configuration;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Auth.Queries;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Auth.Commands.ChangePassword;
using MRC.Agendia.Application.Auth.Commands.ConfirmEmail;
using MRC.Agendia.Application.Auth.Commands.ForgotPassword;
using MRC.Agendia.Application.Auth.Commands.Login;
using MRC.Agendia.Application.Auth.Commands.Logout;
using MRC.Agendia.Application.Auth.Commands.RefreshToken;
using MRC.Agendia.Application.Auth.Commands.Registration;
using MRC.Agendia.Application.Auth.Commands.ResetPassword;

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

        /// <summary>Public self-registration of a client.</summary>
        [HttpPost("register/client")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.RegisterPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> RegisterClient([FromBody] RegisterClientDto dto)
        {
            var result = await _mediator.Send(new RegisterClientCommand(dto));
            return Ok(result);
        }

        /// <summary>Public self-registration of a BusinessOwner together with their Business and associated Employee.</summary>
        [HttpPost("register/owner")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.RegisterPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> RegisterOwner([FromBody] RegisterOwnerDto dto)
        {
            var result = await _mediator.Send(new RegisterOwnerCommand(dto));
            return Ok(result);
        }

        /// <summary>Owner only: creates an Employee plus its user account for the owner's own business.</summary>
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

        /// <summary>Authenticates with email and password and returns an access + refresh token pair.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.LoginPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var result = await _mediator.Send(new LoginCommand(dto));
            return Ok(result);
        }

        /// <summary>Exchanges a refresh token for a new access + refresh token pair (rotation).</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.RefreshPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(dto.RefreshToken));
            return Ok(result);
        }

        /// <summary>Revokes the supplied refresh token for the authenticated user.</summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            await _mediator.Send(new LogoutCommand(dto.RefreshToken, User.GetUserId()));
            return NoContent();
        }

        /// <summary>Revokes all sessions (refresh tokens) of the authenticated user.</summary>
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

        /// <summary>Returns the profile and roles of the authenticated user.</summary>
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

        /// <summary>Changes the authenticated user's password and revokes their other sessions.</summary>
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

        /// <summary>Requests a password-reset link. Always responds 204 (does not reveal whether the email exists).</summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.RegisterPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _mediator.Send(new ForgotPasswordCommand(dto));
            return NoContent();
        }

        /// <summary>Resets the password using the token received by email.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.LoginPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _mediator.Send(new ResetPasswordCommand(dto));
            return NoContent();
        }

        /// <summary>Confirms a user's email with the token sent at registration.</summary>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingSetup.LoginPolicy)]
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
