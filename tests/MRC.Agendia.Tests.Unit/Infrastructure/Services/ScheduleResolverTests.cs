using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Services
{
    /// <summary>
    /// Unit tests for <see cref="ScheduleResolver"/>. The resolver is pure orchestration
    /// over two repositories, so the tests substitute both interfaces and assert the
    /// resulting <see cref="Domain.Services.EffectiveSchedule"/>.
    ///
    /// Precedence under test:
    ///   1. ScheduleOverride (CustomHours / Closed / NationalHoliday / LocalHoliday)
    ///   2. Effective ScheduleTemplate for that date
    ///   3. Fallback: closed with default reason
    /// </summary>
    public class ScheduleResolverTests
    {
        private const int BusinessId = 1;
        private static readonly DateOnly MondayDate = new(2026, 5, 18);

        private readonly IScheduleTemplateRepository _templateRepository = Substitute.For<IScheduleTemplateRepository>();
        private readonly IScheduleOverrideRepository _overrideRepository = Substitute.For<IScheduleOverrideRepository>();
        private readonly ScheduleResolver _sut;

        public ScheduleResolverTests()
        {
            _sut = new ScheduleResolver(_templateRepository, _overrideRepository);
        }

        [Fact]
        public async Task NoOverride_NoTemplate_ReturnsClosedWithDefaultReason()
        {
            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns((ScheduleOverride?)null);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns((ScheduleTemplate?)null);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.Equal(MondayDate, result.Date);
            Assert.False(result.IsOpen);
            Assert.Equal("Sin horario definido", result.ClosedReason);
            Assert.Empty(result.TimeSlots);
            Assert.Null(result.OverrideType);
        }

        [Fact]
        public async Task NoOverride_TemplateWithoutSlotsForThatWeekday_ReturnsClosedDayOff()
        {
            // Template defines slots only for Sunday, but we query Monday.
            var template = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Sunday, new TimeOnly(10, 0), new TimeOnly(14, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns((ScheduleOverride?)null);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns(template);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.False(result.IsOpen);
            Assert.Equal("Dia no laborable", result.ClosedReason);
            Assert.Empty(result.TimeSlots);
        }

        [Fact]
        public async Task NoOverride_TemplateWithSingleSlot_ReturnsOpenWithThatSlot()
        {
            var template = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns((ScheduleOverride?)null);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns(template);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.True(result.IsOpen);
            Assert.Null(result.ClosedReason);
            var slot = Assert.Single(result.TimeSlots);
            Assert.Equal(new TimeOnly(9, 0), slot.StartTime);
            Assert.Equal(new TimeOnly(13, 0), slot.EndTime);
        }

        [Fact]
        public async Task NoOverride_TemplateWithSplitShift_ReturnsTwoSlotsOrderedByStart()
        {
            // Two slots for the same Monday, deliberately inserted in reverse order
            // to verify the resolver orders by StartTime.
            var template = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(16, 0), new TimeOnly(20, 0)),
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns((ScheduleOverride?)null);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns(template);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.True(result.IsOpen);
            Assert.Equal(2, result.TimeSlots.Count);
            Assert.Equal(new TimeOnly(9, 0), result.TimeSlots[0].StartTime);
            Assert.Equal(new TimeOnly(13, 0), result.TimeSlots[0].EndTime);
            Assert.Equal(new TimeOnly(16, 0), result.TimeSlots[1].StartTime);
            Assert.Equal(new TimeOnly(20, 0), result.TimeSlots[1].EndTime);
        }

        [Fact]
        public async Task NoOverride_TemplateWithSlotsForOtherDays_FiltersToThatDayOfWeek()
        {
            var template = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                CreateWeeklySlot(DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(14, 0)),
                CreateWeeklySlot(DayOfWeek.Wednesday, new TimeOnly(11, 0), new TimeOnly(15, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns((ScheduleOverride?)null);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns(template);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.True(result.IsOpen);
            var slot = Assert.Single(result.TimeSlots);
            Assert.Equal(new TimeOnly(9, 0), slot.StartTime);
            Assert.Equal(new TimeOnly(13, 0), slot.EndTime);
        }

        [Fact]
        public async Task OverrideCustomHours_ReturnsOpenWithCustomSlotsOrderedByStart()
        {
            // Reverse order to confirm the resolver sorts custom slots too.
            var customHoursOverride = CreateOverride(ScheduleOverrideType.CustomHours, reason: null, customSlots: new[]
            {
                CreateCustomSlot(new TimeOnly(17, 0), new TimeOnly(21, 0)),
                CreateCustomSlot(new TimeOnly(8, 0), new TimeOnly(12, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns(customHoursOverride);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.True(result.IsOpen);
            Assert.Equal(ScheduleOverrideType.CustomHours, result.OverrideType);
            Assert.Null(result.ClosedReason);
            Assert.Equal(2, result.TimeSlots.Count);
            Assert.Equal(new TimeOnly(8, 0), result.TimeSlots[0].StartTime);
            Assert.Equal(new TimeOnly(17, 0), result.TimeSlots[1].StartTime);

            // When an override applies, the template repository must not be queried.
            await _templateRepository.DidNotReceiveWithAnyArgs().GetEffectiveTemplateAsync(default, default);
        }

        [Theory]
        [InlineData(ScheduleOverrideType.Closed, "Cerrado por inventario", "Cerrado por inventario")]
        [InlineData(ScheduleOverrideType.Closed, null, "Closed")]
        [InlineData(ScheduleOverrideType.NationalHoliday, null, "NationalHoliday")]
        [InlineData(ScheduleOverrideType.LocalHoliday, "San Isidro", "San Isidro")]
        public async Task OverrideClosedKinds_ReturnClosedWithReasonOrEnumName(
            ScheduleOverrideType overrideType,
            string? reason,
            string expectedClosedReason)
        {
            var closedOverride = CreateOverride(overrideType, reason);

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns(closedOverride);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.False(result.IsOpen);
            Assert.Equal(overrideType, result.OverrideType);
            Assert.Equal(expectedClosedReason, result.ClosedReason);
            Assert.Empty(result.TimeSlots);

            // Closed-kind overrides also short-circuit before reading the template.
            await _templateRepository.DidNotReceiveWithAnyArgs().GetEffectiveTemplateAsync(default, default);
        }

        [Fact]
        public async Task ClosedOverride_TakesPrecedenceOverActiveTemplate()
        {
            var closedOverride = CreateOverride(ScheduleOverrideType.Closed, reason: "Cerrado por obras");
            var templateWithSlots = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            });

            _overrideRepository.GetByBusinessIdAndDateAsync(BusinessId, MondayDate)
                .Returns(closedOverride);
            _templateRepository.GetEffectiveTemplateAsync(BusinessId, MondayDate)
                .Returns(templateWithSlots);

            var result = await _sut.GetEffectiveScheduleAsync(BusinessId, MondayDate);

            Assert.False(result.IsOpen);
            Assert.Equal("Cerrado por obras", result.ClosedReason);
            Assert.Empty(result.TimeSlots);
        }

        [Fact]
        public async Task GetEffectiveSchedulesAsync_IteratesEachDayInTheRangeInclusive()
        {
            var from = new DateOnly(2026, 5, 18); // Monday
            var to = new DateOnly(2026, 5, 20);   // Wednesday (inclusive => 3 days)

            // Monday is covered by a template (Monday slot), Tuesday by a CustomHours
            // override, Wednesday by nothing. GetEffectiveSchedulesAsync loads the
            // whole range once (templates + overrides) and resolves it in memory.
            var mondayTemplate = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            });
            mondayTemplate.EffectiveTo = from; // covers Monday but not Wednesday

            var tuesdayOverride = CreateOverride(ScheduleOverrideType.CustomHours, reason: null, customSlots: new[]
            {
                CreateCustomSlot(new TimeOnly(10, 0), new TimeOnly(14, 0)),
            });
            tuesdayOverride.Date = from.AddDays(1);

            _templateRepository.GetByBusinessIdAsync(BusinessId)
                .Returns(new List<ScheduleTemplate> { mondayTemplate });
            _overrideRepository.GetByBusinessIdAndDateRangeAsync(BusinessId, from, to)
                .Returns(new List<ScheduleOverride> { tuesdayOverride });

            var results = (await _sut.GetEffectiveSchedulesAsync(BusinessId, from, to)).ToList();

            Assert.Equal(3, results.Count);

            Assert.True(results[0].IsOpen);
            Assert.Equal(from, results[0].Date);
            Assert.Single(results[0].TimeSlots);

            Assert.True(results[1].IsOpen);
            Assert.Equal(from.AddDays(1), results[1].Date);
            Assert.Equal(ScheduleOverrideType.CustomHours, results[1].OverrideType);

            Assert.False(results[2].IsOpen);
            Assert.Equal(to, results[2].Date);
            Assert.Equal("Sin horario definido", results[2].ClosedReason);
        }

        [Fact]
        public void Resolve_TwoTemplatesCoverDate_DefaultWinsTheTieBreak()
        {
            // Templates do not overlap in valid data, but the resolver must still be
            // deterministic if two cover a date: the default one wins the tie-break.
            var defaultTemplate = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(14, 0)),
            });
            defaultTemplate.IsDefault = true;

            var otherTemplate = CreateTemplate(weeklySlots: new[]
            {
                CreateWeeklySlot(DayOfWeek.Monday, new TimeOnly(16, 0), new TimeOnly(20, 0)),
            });
            otherTemplate.Id = 101;
            otherTemplate.IsDefault = false;

            // Non-default passed first: ordering, not input order, must decide.
            var result = _sut.Resolve(
                new[] { otherTemplate, defaultTemplate }, Array.Empty<ScheduleOverride>(), MondayDate);

            Assert.True(result.IsOpen);
            Assert.Single(result.TimeSlots);
            Assert.Equal(new TimeOnly(10, 0), result.TimeSlots[0].StartTime); // the default template's slot
        }

        // ----- helpers -----

        private static ScheduleTemplate CreateTemplate(IEnumerable<WeeklyTimeSlot> weeklySlots) => new()
        {
            Id = 100,
            BusinessId = BusinessId,
            Name = "Test template",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 12, 31),
            IsDefault = true,
            WeeklySlots = weeklySlots.ToList(),
        };

        private static WeeklyTimeSlot CreateWeeklySlot(DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end) => new()
        {
            DayOfWeek = dayOfWeek,
            StartTime = start,
            EndTime = end,
            SlotType = TimeSlotType.Regular,
        };

        private static ScheduleOverride CreateOverride(
            ScheduleOverrideType overrideType,
            string? reason,
            IEnumerable<CustomTimeSlot>? customSlots = null) => new()
            {
                Id = 200,
                BusinessId = BusinessId,
                Date = MondayDate,
                OverrideType = overrideType,
                Reason = reason,
                CustomSlots = (customSlots ?? Enumerable.Empty<CustomTimeSlot>()).ToList(),
            };

        private static CustomTimeSlot CreateCustomSlot(TimeOnly start, TimeOnly end) => new()
        {
            StartTime = start,
            EndTime = end,
        };
    }
}
