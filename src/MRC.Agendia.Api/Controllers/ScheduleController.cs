using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Schedules.Commands;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Schedules.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:int}/schedules")]
    [Produces("application/json")]
    public class ScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ScheduleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Templates

        [Authorize]
        [HttpGet("templates")]
        [ProducesResponseType(typeof(IEnumerable<ScheduleTemplateDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ScheduleTemplateDto>>> GetTemplates(int businessId)
        {
            var result = await _mediator.Send(new GetScheduleTemplatesByBusinessIdQuery(businessId));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("templates/{templateId}")]
        [ProducesResponseType(typeof(ScheduleTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ScheduleTemplateDto>> GetTemplateById(int businessId, int templateId)
        {
            var result = await _mediator.Send(new GetScheduleTemplateByIdQuery(templateId));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("templates")]
        [ProducesResponseType(typeof(ScheduleTemplateDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ScheduleTemplateDto>> CreateTemplate(int businessId, [FromBody] CreateScheduleTemplateDto dto)
        {
            if (dto.BusinessId != businessId) return BadRequest("BusinessId in URL and body must match.");
            var result = await _mediator.Send(new CreateScheduleTemplateCommand(dto));
            return CreatedAtAction(nameof(GetTemplateById), new { businessId, templateId = result.Id }, result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPut("templates/{templateId}")]
        [ProducesResponseType(typeof(ScheduleTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ScheduleTemplateDto>> UpdateTemplate(int businessId, int templateId, [FromBody] UpdateScheduleTemplateDto dto)
        {
            if (dto.Id != templateId) return BadRequest("Template Id in URL and body must match.");
            var result = await _mediator.Send(new UpdateScheduleTemplateCommand(dto));
            return Ok(result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpDelete("templates/{templateId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTemplate(int businessId, int templateId)
        {
            await _mediator.Send(new DeleteScheduleTemplateCommand(templateId));
            return NoContent();
        }

        #endregion

        #region Generate

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateScheduleResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GenerateScheduleResponseDto>> GenerateSchedule(int businessId, [FromBody] GenerateScheduleRequestDto dto)
        {
            if (dto.BusinessId != businessId) return BadRequest("BusinessId in URL and body must match.");
            var result = await _mediator.Send(new GenerateScheduleCommand(dto));
            return Created($"api/businesses/{businessId}/schedules/templates", result);
        }

        /// <summary>Previsualiza el calendario resultante de un generate sin persistir nada.</summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("preview")]
        [ProducesResponseType(typeof(IEnumerable<CalendarDayDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<CalendarDayDto>>> PreviewSchedule(int businessId, [FromBody] GenerateScheduleRequestDto dto)
        {
            if (dto.BusinessId != businessId) return BadRequest("BusinessId in URL and body must match.");
            var result = await _mediator.Send(new PreviewScheduleQuery(dto));
            return Ok(result);
        }

        #endregion

        #region Overrides

        [Authorize]
        [HttpGet("overrides")]
        [ProducesResponseType(typeof(IEnumerable<ScheduleOverrideDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ScheduleOverrideDto>>> GetOverrides(int businessId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var result = await _mediator.Send(new GetScheduleOverridesByBusinessIdQuery(businessId, from, to));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("overrides/{overrideId}")]
        [ProducesResponseType(typeof(ScheduleOverrideDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ScheduleOverrideDto>> GetOverrideById(int businessId, int overrideId)
        {
            var result = await _mediator.Send(new GetScheduleOverrideByIdQuery(overrideId));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost("overrides")]
        [ProducesResponseType(typeof(ScheduleOverrideDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ScheduleOverrideDto>> CreateOverride(int businessId, [FromBody] CreateScheduleOverrideDto dto)
        {
            if (dto.BusinessId != businessId) return BadRequest("BusinessId in URL and body must match.");
            var result = await _mediator.Send(new CreateScheduleOverrideCommand(dto));
            return CreatedAtAction(nameof(GetOverrideById), new { businessId, overrideId = result.Id }, result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPut("overrides/{overrideId}")]
        [ProducesResponseType(typeof(ScheduleOverrideDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ScheduleOverrideDto>> UpdateOverride(int businessId, int overrideId, [FromBody] UpdateScheduleOverrideDto dto)
        {
            if (dto.Id != overrideId) return BadRequest("Override Id in URL and body must match.");
            var result = await _mediator.Send(new UpdateScheduleOverrideCommand(dto));
            return Ok(result);
        }

        [Authorize(Roles = RolePolicies.Staff)]
        [HttpDelete("overrides/{overrideId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOverride(int businessId, int overrideId)
        {
            await _mediator.Send(new DeleteScheduleOverrideCommand(overrideId));
            return NoContent();
        }

        #endregion

        #region Effective Schedule

        [Authorize]
        [HttpGet("effective")]
        [ProducesResponseType(typeof(EffectiveScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EffectiveScheduleDto>> GetEffectiveSchedule(int businessId, [FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetEffectiveScheduleQuery(businessId, date));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(IEnumerable<CalendarDayDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<CalendarDayDto>>> GetCalendar(int businessId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var result = await _mediator.Send(new GetCalendarQuery(businessId, from, to));
            return Ok(result);
        }

        #endregion
    }
}
