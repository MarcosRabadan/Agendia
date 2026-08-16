using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Queries.CancellationPolicy
{
    public class GetCancellationPolicyQueryHandler
        : IRequestHandler<GetCancellationPolicyQuery, IReadOnlyList<CancellationPolicyTierDto>>
    {
        private readonly IBusinessRepository _repository;

        public GetCancellationPolicyQueryHandler(IBusinessRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<CancellationPolicyTierDto>> Handle(GetCancellationPolicyQuery request,
                                                                           CancellationToken cancellationToken)
        {
            // Readable by any authenticated caller: a client needs to know what cancelling
            // will cost them before they book, the same way they see the schedule.
            var tiers = await _repository.GetCancellationTiersAsync(request.BusinessId, cancellationToken);

            return tiers
                .Select(t => new CancellationPolicyTierDto(t.MinHoursBefore, t.PenaltyKind, t.PenaltyValue))
                .ToList();
        }
    }
}
