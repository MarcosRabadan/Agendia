using MRC.Agendia.Application.Schedules.Commands.Generation;
using MRC.Agendia.Application.Schedules.Commands.Overrides;
using MRC.Agendia.Application.Schedules.Commands.Slots;
using MRC.Agendia.Application.Schedules.Commands.Templates;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Schedules.Queries.Calendar;
using MRC.Agendia.Application.Schedules.Queries.Preview;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Validators
{
    /// <summary>
    /// Rule coverage for the scheduling validators: weekly/custom slots (time order,
    /// enums), templates and overrides (ids, date order, slot presence, intra-day
    /// overlap, CustomHours requirements), generation (year bounds, nested items) and
    /// the calendar range caps.
    /// </summary>
    public class ScheduleValidatorsTests
    {
        private static readonly DateOnly From = new(2026, 1, 1);
        private static readonly DateOnly To = new(2026, 6, 30);

        private static CreateWeeklyTimeSlotDto Slot(int startH = 9, int endH = 13, DayOfWeek day = DayOfWeek.Monday) =>
            new(day, new TimeOnly(startH, 0), new TimeOnly(endH, 0), TimeSlotType.Regular);

        // ---------- Weekly slot ----------

        [Fact]
        public void WeeklySlot_valid_passes()
            => new CreateWeeklyTimeSlotDtoValidator().Check(Slot()).ShouldBeValid();

        [Fact]
        public void WeeklySlot_end_not_after_start_fails()
            => new CreateWeeklyTimeSlotDtoValidator().Check(Slot() with { EndTime = new TimeOnly(9, 0) }).ShouldFailOn("EndTime");

        [Fact]
        public void WeeklySlot_day_out_of_enum_fails()
            => new CreateWeeklyTimeSlotDtoValidator().Check(Slot() with { DayOfWeek = (DayOfWeek)99 }).ShouldFailOn("DayOfWeek");

        [Fact]
        public void WeeklySlot_slot_type_out_of_enum_fails()
            => new CreateWeeklyTimeSlotDtoValidator().Check(Slot() with { SlotType = (TimeSlotType)99 }).ShouldFailOn("SlotType");

        // ---------- Custom slot ----------

        [Fact]
        public void CustomSlot_valid_passes()
            => new CreateCustomTimeSlotDtoValidator().Check(new CreateCustomTimeSlotDto(new TimeOnly(9, 0), new TimeOnly(13, 0))).ShouldBeValid();

        [Fact]
        public void CustomSlot_end_not_after_start_fails()
            => new CreateCustomTimeSlotDtoValidator().Check(new CreateCustomTimeSlotDto(new TimeOnly(13, 0), new TimeOnly(9, 0))).ShouldFailOn("EndTime");

        // ---------- Closed date ----------

        [Fact]
        public void ClosedDate_valid_passes()
            => new ClosedDateDtoValidator().Check(new ClosedDateDto(From, "Puente")).ShouldBeValid();

        [Fact]
        public void ClosedDate_default_date_fails()
            => new ClosedDateDtoValidator().Check(new ClosedDateDto(default, null)).ShouldFailOn("Date");

        // ---------- Templates ----------

        private static CreateScheduleTemplateDto ValidTemplate() =>
            new(7, "Curso", From, To, false, new List<CreateWeeklyTimeSlotDto> { Slot() });

        [Fact]
        public void CreateTemplate_valid_passes()
            => new CreateScheduleTemplateCommandValidator().Check(new CreateScheduleTemplateCommand(ValidTemplate())).ShouldBeValid();

        [Fact]
        public void CreateTemplate_business_id_must_be_positive()
            => new CreateScheduleTemplateCommandValidator()
                .Check(new CreateScheduleTemplateCommand(ValidTemplate() with { BusinessId = 0 })).ShouldFailOn("Dto.BusinessId");

        [Fact]
        public void CreateTemplate_effective_to_before_from_fails()
            => new CreateScheduleTemplateCommandValidator()
                .Check(new CreateScheduleTemplateCommand(ValidTemplate() with { EffectiveTo = From.AddDays(-1) })).ShouldFailOn("Dto.EffectiveTo");

        [Fact]
        public void CreateTemplate_no_slots_fails()
            => new CreateScheduleTemplateCommandValidator()
                .Check(new CreateScheduleTemplateCommand(ValidTemplate() with { WeeklySlots = new List<CreateWeeklyTimeSlotDto>() }))
                .ShouldFailOn("Dto.WeeklySlots");

        [Fact]
        public void CreateTemplate_intra_day_overlap_fails()
            => new CreateScheduleTemplateCommandValidator()
                .Check(new CreateScheduleTemplateCommand(ValidTemplate() with
                {
                    WeeklySlots = new List<CreateWeeklyTimeSlotDto> { Slot(9, 13), Slot(12, 15) }
                }))
                .ShouldFailOn("Dto.WeeklySlots");

        [Fact]
        public void UpdateTemplate_valid_passes()
            => new UpdateScheduleTemplateCommandValidator()
                .Check(new UpdateScheduleTemplateCommand(new UpdateScheduleTemplateDto(1, "Curso", From, To, false, new List<CreateWeeklyTimeSlotDto> { Slot() })))
                .ShouldBeValid();

        [Fact]
        public void UpdateTemplate_id_must_be_positive()
            => new UpdateScheduleTemplateCommandValidator()
                .Check(new UpdateScheduleTemplateCommand(new UpdateScheduleTemplateDto(0, "Curso", From, To, false, new List<CreateWeeklyTimeSlotDto> { Slot() })))
                .ShouldFailOn("Dto.Id");

        [Fact]
        public void DeleteTemplate_id_must_be_positive()
            => new DeleteScheduleTemplateCommandValidator().Check(new DeleteScheduleTemplateCommand(0)).ShouldFailOn("Id");

        // ---------- Overrides ----------

        private static CreateScheduleOverrideDto ClosedOverride() =>
            new(7, new DateOnly(2026, 5, 1), ScheduleOverrideType.Closed, "Festivo", null);

        [Fact]
        public void CreateOverride_closed_valid_passes()
            => new CreateScheduleOverrideCommandValidator().Check(new CreateScheduleOverrideCommand(ClosedOverride())).ShouldBeValid();

        [Fact]
        public void CreateOverride_default_date_fails()
            => new CreateScheduleOverrideCommandValidator()
                .Check(new CreateScheduleOverrideCommand(ClosedOverride() with { Date = default })).ShouldFailOn("Dto.Date");

        [Fact]
        public void CreateOverride_type_out_of_enum_fails()
            => new CreateScheduleOverrideCommandValidator()
                .Check(new CreateScheduleOverrideCommand(ClosedOverride() with { OverrideType = (ScheduleOverrideType)99 })).ShouldFailOn("Dto.OverrideType");

        [Fact]
        public void CreateOverride_custom_hours_without_slots_fails()
            => new CreateScheduleOverrideCommandValidator()
                .Check(new CreateScheduleOverrideCommand(ClosedOverride() with { OverrideType = ScheduleOverrideType.CustomHours, CustomSlots = null }))
                .ShouldFailOn("Dto.CustomSlots");

        [Fact]
        public void CreateOverride_custom_hours_with_slots_passes()
            => new CreateScheduleOverrideCommandValidator()
                .Check(new CreateScheduleOverrideCommand(ClosedOverride() with
                {
                    OverrideType = ScheduleOverrideType.CustomHours,
                    CustomSlots = new List<CreateCustomTimeSlotDto> { new(new TimeOnly(9, 0), new TimeOnly(13, 0)) }
                }))
                .ShouldBeValid();

        [Fact]
        public void CreateOverride_custom_hours_overlapping_slots_fails()
            => new CreateScheduleOverrideCommandValidator()
                .Check(new CreateScheduleOverrideCommand(ClosedOverride() with
                {
                    OverrideType = ScheduleOverrideType.CustomHours,
                    CustomSlots = new List<CreateCustomTimeSlotDto>
                    {
                        new(new TimeOnly(9, 0), new TimeOnly(13, 0)),
                        new(new TimeOnly(12, 0), new TimeOnly(15, 0))
                    }
                }))
                .ShouldFailOn("Dto.CustomSlots");

        [Fact]
        public void UpdateOverride_id_must_be_positive()
            => new UpdateScheduleOverrideCommandValidator()
                .Check(new UpdateScheduleOverrideCommand(new UpdateScheduleOverrideDto(0, new DateOnly(2026, 5, 1), ScheduleOverrideType.Closed, null, null)))
                .ShouldFailOn("Dto.Id");

        [Fact]
        public void DeleteOverride_id_must_be_positive()
            => new DeleteScheduleOverrideCommandValidator().Check(new DeleteScheduleOverrideCommand(0)).ShouldFailOn("Id");

        // ---------- Generation ----------

        private static GenerateScheduleRequestDto ValidRequest() =>
            new(7, 2026,
                new List<GenerateScheduleTemplateInputDto> { new("Curso", From, To, false, new List<CreateWeeklyTimeSlotDto> { Slot() }) },
                IncludeNationalHolidays: true, IncludeLocalHolidays: false,
                VacationPeriods: null, CustomClosedDates: null);

        [Fact]
        public void Generate_valid_passes()
            => new GenerateScheduleCommandValidator().Check(new GenerateScheduleCommand(ValidRequest())).ShouldBeValid();

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        public void Generate_year_out_of_range_fails(int year)
            => new GenerateScheduleRequestDtoValidator().Check(ValidRequest() with { Year = year }).ShouldFailOn("Year");

        [Fact]
        public void Generate_no_templates_fails()
            => new GenerateScheduleRequestDtoValidator()
                .Check(ValidRequest() with { Templates = new List<GenerateScheduleTemplateInputDto>() }).ShouldFailOn("Templates");

        [Fact]
        public void Generate_nested_vacation_invalid_fails()
            => new GenerateScheduleRequestDtoValidator()
                .Check(ValidRequest() with { VacationPeriods = new List<VacationPeriodDto> { new(To, From, null) } })
                .ShouldFailOn("VacationPeriods[0].To");

        [Fact]
        public void Preview_wraps_request_validation()
            => new PreviewScheduleQueryValidator().Check(new PreviewScheduleQuery(ValidRequest() with { BusinessId = 0 })).ShouldFailOn("Dto.BusinessId");

        // ---------- Calendar ----------

        [Fact]
        public void Calendar_valid_passes()
            => new GetCalendarQueryValidator().Check(new GetCalendarQuery(7, From, To)).ShouldBeValid();

        [Fact]
        public void Calendar_to_before_from_fails()
            => new GetCalendarQueryValidator().Check(new GetCalendarQuery(7, To, From)).ShouldFailOn("To");

        [Fact]
        public void Calendar_over_366_days_fails()
            => new GetCalendarQueryValidator().Check(new GetCalendarQuery(7, From, From.AddDays(400))).ShouldFailOn("");

        [Fact]
        public void Calendar_date_at_max_value_fails()
            => new GetCalendarQueryValidator().Check(new GetCalendarQuery(7, DateOnly.MaxValue, DateOnly.MaxValue)).ShouldFailOn("From");
    }
}
