using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Holidays.Commands;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Application.Holidays.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HolidayController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HolidayController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HolidayCalendarDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<HolidayCalendarDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllHolidaysQuery());
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("year/{year}")]
        [ProducesResponseType(typeof(IEnumerable<HolidayCalendarDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<HolidayCalendarDto>>> GetByYear(int year)
        {
            var result = await _mediator.Send(new GetHolidaysByYearQuery(year));
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(HolidayCalendarDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HolidayCalendarDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetHolidayByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(HolidayCalendarDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<HolidayCalendarDto>> Create([FromBody] CreateHolidayCalendarDto dto)
        {
            var result = await _mediator.Send(new CreateHolidayCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HolidayCalendarDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HolidayCalendarDto>> Update(int id, [FromBody] UpdateHolidayCalendarDto dto)
        {
            if (dto.Id != id) return BadRequest("Holiday Id in URL and body must match.");
            var result = await _mediator.Send(new UpdateHolidayCommand(dto));
            return Ok(result);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteHolidayCommand(id));
            return NoContent();
        }
    }
}
