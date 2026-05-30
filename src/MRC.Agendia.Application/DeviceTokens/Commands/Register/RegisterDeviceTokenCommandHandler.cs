using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.DeviceTokens.Commands.Register
{
    public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand, bool>
    {
        private readonly IDeviceTokenRepository _repository;
        private readonly ICurrentUserContext _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterDeviceTokenCommandHandler(
            IDeviceTokenRepository repository,
            ICurrentUserContext currentUser,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            var userId = RequireUserId();

            // One row per token: if the same device re-registers (possibly under a
            // different account), re-point it at the caller instead of duplicating.
            var existing = await _repository.GetByTokenAsync(request.Dto.Token, cancellationToken);
            if (existing is not null)
            {
                existing.UserId = userId;
                existing.Platform = request.Dto.Platform;
                _repository.Update(existing);
            }
            else
            {
                await _repository.AddAsync(new DeviceToken
                {
                    UserId = userId,
                    Token = request.Dto.Token,
                    Platform = request.Dto.Platform
                }, cancellationToken);
            }

            await _unitOfWork.Save(cancellationToken);
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
