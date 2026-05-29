using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.DeviceTokens.Commands
{
    public class RemoveDeviceTokenCommandHandler : IRequestHandler<RemoveDeviceTokenCommand, bool>
    {
        private readonly IDeviceTokenRepository _repository;
        private readonly ICurrentUserContext _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveDeviceTokenCommandHandler(
            IDeviceTokenRepository repository,
            ICurrentUserContext currentUser,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RemoveDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            var userId = RequireUserId();

            // Idempotent: only delete the token if it exists AND belongs to the
            // caller, so one user cannot unregister another user's device.
            var existing = await _repository.GetByTokenAsync(request.Dto.Token, cancellationToken);
            if (existing is not null && existing.UserId == userId)
            {
                _repository.Delete(existing);
                await _unitOfWork.Save(cancellationToken);
            }

            return true;
        }

        private string RequireUserId()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            return _currentUser.UserId!;
        }
    }
}
