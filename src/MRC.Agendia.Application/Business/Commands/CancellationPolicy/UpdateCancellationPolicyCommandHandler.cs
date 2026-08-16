using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Commands.CancellationPolicy
{
    public class UpdateCancellationPolicyCommandHandler
        : IRequestHandler<UpdateCancellationPolicyCommand, IReadOnlyList<CancellationPolicyTierDto>>
    {
        private readonly IBusinessRepository _repository;
        private readonly IResourceAuthorizationService _auth;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCancellationPolicyCommandHandler(IBusinessRepository repository,
                                                      IResourceAuthorizationService auth,
                                                      IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _auth = auth;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<CancellationPolicyTierDto>> Handle(UpdateCancellationPolicyCommand request,
                                                                           CancellationToken cancellationToken)
        {
            // Setting the policy is owner/admin territory, like editing the business itself.
            await _auth.EnsureCanManageBusinessAsync(request.BusinessId, cancellationToken);

            var tiers = request.Dto.Tiers
                .Select(t => new CancellationPolicyTier
                {
                    MinHoursBefore = t.MinHoursBefore,
                    PenaltyKind = t.PenaltyKind,
                    PenaltyValue = t.PenaltyValue
                })
                .ToList();

            await _repository.ReplaceCancellationTiersAsync(request.BusinessId, tiers, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            var saved = await _repository.GetCancellationTiersAsync(request.BusinessId, cancellationToken);
            return saved
                .Select(t => new CancellationPolicyTierDto(t.MinHoursBefore, t.PenaltyKind, t.PenaltyValue))
                .ToList();
        }
    }
}
