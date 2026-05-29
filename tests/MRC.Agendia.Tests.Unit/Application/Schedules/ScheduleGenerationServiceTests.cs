using AutoMapper;
using MRC.Agendia.Application.Schedules;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Schedules
{
    /// <summary>
    /// Generation must not emit two ScheduleOverrides for the same (business, date):
    /// overlapping vacations, a closure repeated or falling on a vacation/holiday, or
    /// a date that already has an override in the DB would otherwise violate the
    /// unique index IX_ScheduleOverride_BusinessId_Date (HTTP 500). See #188.
    /// </summary>
    public class ScheduleGenerationServiceTests
    {
        private readonly IScheduleTemplateRepository _templateRepo = Substitute.For<IScheduleTemplateRepository>();
        private readonly IScheduleOverrideRepository _overrideRepo = Substitute.For<IScheduleOverrideRepository>();
        private readonly IHolidayCalendarRepository _holidayRepo = Substitute.For<IHolidayCalendarRepository>();
        private readonly IScheduleResolver _resolver = Substitute.For<IScheduleResolver>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly ScheduleGenerationService _sut;

        public ScheduleGenerationServiceTests()
        {
            _sut = new ScheduleGenerationService(
                _templateRepo, _overrideRepo, _holidayRepo, _resolver, _unitOfWork, _mapper);

            _templateRepo.HasOverlappingTemplateAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Returns(false);
            _mapper.Map<ScheduleTemplate>(Arg.Any<GenerateScheduleTemplateInputDto>())
                .Returns(_ => new ScheduleTemplate());
        }

        [Fact]
        public async Task Generate_DeduplicaFechas_YNoColisionaConOverridesExistentes()
        {
            // An override already exists for 2030-07-15 (e.g. a manual closure or a
            // previous generation).
            _overrideRepo.GetByBusinessIdAndDateRangeAsync(1, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleOverride> { new() { BusinessId = 1, Date = new DateOnly(2030, 7, 15) } });

            var template = new GenerateScheduleTemplateInputDto(
                "Anual", new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31), true,
                new List<CreateWeeklyTimeSlotDto>
                {
                    new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), default)
                });

            var request = new GenerateScheduleRequestDto(
                BusinessId: 1,
                Year: 2030,
                Templates: new List<GenerateScheduleTemplateInputDto> { template },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: new List<VacationPeriodDto>
                {
                    new(new DateOnly(2030, 7, 10), new DateOnly(2030, 7, 12), "Vacaciones A"),
                    new(new DateOnly(2030, 7, 11), new DateOnly(2030, 7, 13), "Vacaciones B (solapa A)")
                },
                CustomClosedDates: new List<ClosedDateDto>
                {
                    new(new DateOnly(2030, 7, 12), "Cierre que coincide con vacaciones"),
                    new(new DateOnly(2030, 7, 15), "Cierre que coincide con override existente"),
                    new(new DateOnly(2030, 7, 20), "Cierre nuevo"),
                    new(new DateOnly(2030, 7, 20), "Cierre nuevo duplicado")
                });

            await _sut.GenerateScheduleAsync(request);

            var addRange = _overrideRepo.ReceivedCalls()
                .Single(c => c.GetMethodInfo().Name == nameof(IScheduleOverrideRepository.AddRangeAsync));
            var inserted = ((IEnumerable<ScheduleOverride>)addRange.GetArguments()[0]!).ToList();

            // No duplicate (business, date): every inserted override has a distinct date.
            Assert.Equal(inserted.Count, inserted.Select(o => o.Date).Distinct().Count());
            // The date that already had an override in the DB is not re-inserted.
            Assert.DoesNotContain(inserted, o => o.Date == new DateOnly(2030, 7, 15));
            // Concretely: 07-10, 07-11, 07-12, 07-13 (vacations, deduped) + 07-20 (closure).
            Assert.Equal(5, inserted.Count);
        }
    }
}
