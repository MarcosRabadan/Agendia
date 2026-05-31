using AutoMapper;
using MRC.Agendia.Application.Schedules;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Schedules
{
    /// <summary>
    /// Schedule generation behaviour:
    /// - Within a single request, overlapping vacations / repeated closures dedupe
    ///   by date so two ScheduleOverrides never share a (business, date), which would
    ///   violate the unique index IX_ScheduleOverride_BusinessId_Date (HTTP 500, #188).
    /// - Generating over a year the business already has a schedule for is refused
    ///   unless ReplaceExisting is set (warn-before-overwrite), and when confirmed it
    ///   deletes the year's schedule and recreates it (no stale "ghost" days linger).
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

            // Default: the business has no existing schedule for the year.
            _templateRepo.GetByBusinessIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate>());
            _overrideRepo.GetByBusinessIdAndDateRangeAsync(
                    Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleOverride>());

            // Run the transactional work inline so the replace path actually executes.
            _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ((Func<Task>)ci[0]).Invoke());
        }

        private static GenerateScheduleTemplateInputDto AnnualTemplate(int year)
            => new("Anual", new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), true,
                new List<CreateWeeklyTimeSlotDto>
                {
                    new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), default)
                });

        [Fact]
        public async Task Generate_DeduplicaFechasDentroDeLaMismaPeticion()
        {
            // Clean year (no existing schedule) so it persists directly.
            var request = new GenerateScheduleRequestDto(
                BusinessId: 1,
                Year: 2030,
                Templates: new List<GenerateScheduleTemplateInputDto> { AnnualTemplate(2030) },
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
                    new(new DateOnly(2030, 7, 20), "Cierre nuevo"),
                    new(new DateOnly(2030, 7, 20), "Cierre nuevo duplicado")
                });

            await _sut.GenerateScheduleAsync(request);

            var addRange = _overrideRepo.ReceivedCalls()
                .Single(c => c.GetMethodInfo().Name == nameof(IScheduleOverrideRepository.AddRangeAsync));
            var inserted = ((IEnumerable<ScheduleOverride>)addRange.GetArguments()[0]!).ToList();

            // No duplicate (business, date): every inserted override has a distinct date.
            Assert.Equal(inserted.Count, inserted.Select(o => o.Date).Distinct().Count());
            // 07-10, 07-11, 07-12, 07-13 (vacations, deduped) + 07-20 (closure, deduped).
            Assert.Equal(5, inserted.Count);
        }

        [Fact]
        public async Task Generate_SinReemplazo_CuandoYaHayHorario_Lanza_YNoPersiste()
        {
            // The business already has an override that year.
            _overrideRepo.GetByBusinessIdAndDateRangeAsync(1, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleOverride> { new() { BusinessId = 1, Date = new DateOnly(2030, 6, 20) } });

            var request = new GenerateScheduleRequestDto(
                BusinessId: 1,
                Year: 2030,
                Templates: new List<GenerateScheduleTemplateInputDto> { AnnualTemplate(2030) },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null,
                ReplaceExisting: false);

            await Assert.ThrowsAsync<ScheduleAlreadyExistsForYearException>(() => _sut.GenerateScheduleAsync(request));

            // Nothing was created and nothing was deleted.
            await _overrideRepo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<ScheduleOverride>>(), Arg.Any<CancellationToken>());
            await _templateRepo.DidNotReceive().AddAsync(Arg.Any<ScheduleTemplate>(), Arg.Any<CancellationToken>());
            _overrideRepo.DidNotReceive().Delete(Arg.Any<ScheduleOverride>());
            _templateRepo.DidNotReceive().Delete(Arg.Any<ScheduleTemplate>());
        }

        [Fact]
        public async Task Generate_ConReemplazo_BorraLoExistente_YAplicaLasNuevasVacaciones()
        {
            // Stale schedule from a previous run: a template covering the year and a
            // closure on 06-20 (the kind of "ghost" day that used to linger).
            var staleTemplate = new ScheduleTemplate
            {
                Id = 99,
                BusinessId = 1,
                EffectiveFrom = new DateOnly(2030, 1, 1),
                EffectiveTo = new DateOnly(2030, 12, 31)
            };
            var staleOverride = new ScheduleOverride { Id = 50, BusinessId = 1, Date = new DateOnly(2030, 6, 20) };
            _templateRepo.GetByBusinessIdAsync(1, Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { staleTemplate });
            _overrideRepo.GetByBusinessIdAndDateRangeAsync(1, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleOverride> { staleOverride });

            var request = new GenerateScheduleRequestDto(
                BusinessId: 1,
                Year: 2030,
                Templates: new List<GenerateScheduleTemplateInputDto> { AnnualTemplate(2030) },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: new List<VacationPeriodDto>
                {
                    // Overlaps the stale 06-20: under the old additive behaviour this
                    // would have been skipped; with replace it must be applied.
                    new(new DateOnly(2030, 6, 18), new DateOnly(2030, 6, 20), "Vacaciones nuevas")
                },
                CustomClosedDates: null,
                ReplaceExisting: true);

            var response = await _sut.GenerateScheduleAsync(request);

            // The stale template and override were deleted.
            _templateRepo.Received(1).Delete(staleTemplate);
            _overrideRepo.Received(1).Delete(staleOverride);

            // The new vacations were inserted, including 06-20 (no longer skipped).
            var addRange = _overrideRepo.ReceivedCalls()
                .Single(c => c.GetMethodInfo().Name == nameof(IScheduleOverrideRepository.AddRangeAsync));
            var inserted = ((IEnumerable<ScheduleOverride>)addRange.GetArguments()[0]!).ToList();
            Assert.Equal(3, inserted.Count); // 06-18, 06-19, 06-20
            Assert.Contains(inserted, o => o.Date == new DateOnly(2030, 6, 20));

            // The response reports the replacement.
            Assert.NotNull(response.Warnings);
            Assert.Contains(response.Warnings!, w => w.Contains("reemplaz", StringComparison.OrdinalIgnoreCase));
        }
    }
}
