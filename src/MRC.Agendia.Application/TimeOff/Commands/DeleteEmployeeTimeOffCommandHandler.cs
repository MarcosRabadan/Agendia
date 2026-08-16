using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    public class DeleteEmployeeTimeOffCommandHandler : IRequestHandler<DeleteEmployeeTimeOffCommand>
    {
        private readonly IEmployeeTimeOffRepository _repository;
        private readonly IResourceAuthorizationService _auth;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteEmployeeTimeOffCommandHandler(IEmployeeTimeOffRepository repository,
                                                   IResourceAuthorizationService auth,
                                                   IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _auth = auth;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteEmployeeTimeOffCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanUpdateEmployeeAsync(request.EmployeeId, cancellationToken);

            var timeOff = await _repository.GetByIdAsync(request.TimeOffId, cancellationToken);

            // Mismatched employee is treated as missing: the block does not belong to the
            // agenda the caller was authorized for.
            if (timeOff is null || timeOff.EmployeeId != request.EmployeeId)
                throw new EmployeeTimeOffNotFoundException(request.TimeOffId);

            _repository.Delete(timeOff);
            await _unitOfWork.Save(cancellationToken);
        }
    }
}
