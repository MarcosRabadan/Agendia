using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetClientReliabilityQueryHandler : IRequestHandler<GetClientReliabilityQuery, ClientReliabilityDto>
    {
        private readonly IBusinessStatsRepository _repository;
        private readonly IResourceAuthorizationService _auth;
        private readonly IClock _clock;

        public GetClientReliabilityQueryHandler(IBusinessStatsRepository repository,
                                                IResourceAuthorizationService auth,
                                                IClock clock)
        {
            _repository = repository;
            _auth = auth;
            _clock = clock;
        }

        public async Task<ClientReliabilityDto> Handle(GetClientReliabilityQuery request, CancellationToken cancellationToken)
        {
            // Same gate as the stats panel: business staff (owner or an active employee)
            // or an admin. A client can never read another client's record.
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);

            // Window ends "now" (wall clock, like the appointment dates): a future booking
            // has no outcome yet, so counting it would dilute the record.
            var toExclusive = _clock.BusinessNow;
            var fromInclusive = toExclusive.Date.AddDays(-request.Days);

            var statuses = await _repository.GetClientAppointmentStatusesAsync(
                request.BusinessId, request.ClientUserId, fromInclusive, toExclusive, cancellationToken);

            return ClientReliabilityCalculator.Calculate(
                statuses,
                request.BusinessId,
                request.ClientUserId,
                DateOnly.FromDateTime(fromInclusive),
                DateOnly.FromDateTime(toExclusive));
        }
    }
}
