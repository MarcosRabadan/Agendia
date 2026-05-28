using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetBusinessStatsQueryHandler : IRequestHandler<GetBusinessStatsQuery, BusinessStatsDto>
    {
        private readonly IBusinessStatsRepository _repository;
        private readonly IResourceAuthorizationService _auth;

        public GetBusinessStatsQueryHandler(IBusinessStatsRepository repository, IResourceAuthorizationService auth)
        {
            _repository = repository;
            _auth = auth;
        }

        public async Task<BusinessStatsDto> Handle(GetBusinessStatsQuery request, CancellationToken cancellationToken)
        {
            // Only the business owner (or an admin) can see its statistics.
            await _auth.EnsureCanManageBusinessAsync(request.BusinessId, cancellationToken);

            // [From, To] inclusive -> [from 00:00, (to+1) 00:00) half-open window.
            var fromInclusive = request.From.ToDateTime(TimeOnly.MinValue);
            var toExclusive = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var rows = await _repository.GetAppointmentsAsync(request.BusinessId, fromInclusive, toExclusive, cancellationToken);
            return BusinessStatsCalculator.Calculate(rows, request.From, request.To);
        }
    }
}
