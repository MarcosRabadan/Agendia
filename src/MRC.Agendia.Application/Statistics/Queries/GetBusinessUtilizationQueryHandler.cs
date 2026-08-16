using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetBusinessUtilizationQueryHandler : IRequestHandler<GetBusinessUtilizationQuery, UtilizationDto>
    {
        private readonly IBusinessStatsRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeTimeOffRepository _timeOffRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IResourceAuthorizationService _auth;
        private readonly IClock _clock;

        public GetBusinessUtilizationQueryHandler(IBusinessStatsRepository repository,
                                                  IEmployeeRepository employeeRepository,
                                                  IEmployeeTimeOffRepository timeOffRepository,
                                                  IScheduleResolver scheduleResolver,
                                                  IResourceAuthorizationService auth,
                                                  IClock clock)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _timeOffRepository = timeOffRepository;
            _scheduleResolver = scheduleResolver;
            _auth = auth;
            _clock = clock;
        }

        public async Task<UtilizationDto> Handle(GetBusinessUtilizationQuery request, CancellationToken cancellationToken)
        {
            // Same gate as the stats panel: business staff or an admin.
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);

            // [From, To] inclusive -> [from 00:00, (to+1) 00:00) half-open window.
            var fromInclusive = request.From.ToDateTime(TimeOnly.MinValue);
            var toExclusive = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var employees = (await _employeeRepository.GetActiveByBusinessIdAsync(request.BusinessId, cancellationToken))
                .Select(e => new UtilizationEmployee(e.Id, e.MaxConcurrentAppointments))
                .ToList();

            // The effective schedule is the source of truth for what was on offer, so split
            // shifts, overrides and holidays are honoured without reimplementing them. Per
            // day rather than in bulk because the resolver's repositories are cached, and
            // the range is capped at a quarter.
            var schedules = new List<EffectiveSchedule>();
            for (var date = request.From; date <= request.To; date = date.AddDays(1))
                schedules.Add(await _scheduleResolver.GetEffectiveScheduleAsync(request.BusinessId, date, cancellationToken));

            var timeOff = await _timeOffRepository.GetByEmployeesAndRangeAsync(
                employees.Select(e => e.Id).ToList(), fromInclusive, toExclusive, cancellationToken);

            var appointments = await _repository.GetUtilizationAppointmentsAsync(
                request.BusinessId, fromInclusive, toExclusive, cancellationToken);

            // Lead time crosses the two time worlds: CreatedAt is a UTC instant, StartDate a
            // wall-clock value. Convert the instant to the business clock before subtracting,
            // or the result is off by the timezone offset. A negative gap (a row created
            // after its own start, only reachable through a back-dated import) is dropped.
            var leadTimes = appointments
                .Select(a => (a.StartDate - _clock.ToBusinessTime(a.CreatedAtUtc)).TotalHours)
                .Where(hours => hours >= 0)
                .ToList();

            return UtilizationCalculator.Calculate(
                schedules, employees, appointments, timeOff, leadTimes, request.From, request.To);
        }
    }
}
