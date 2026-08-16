using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.TimeOff.Queries
{
    public class GetEmployeeTimeOffQueryHandler
        : IRequestHandler<GetEmployeeTimeOffQuery, IReadOnlyList<EmployeeTimeOffDto>>
    {
        private readonly IEmployeeTimeOffRepository _repository;
        private readonly IResourceAuthorizationService _auth;

        public GetEmployeeTimeOffQueryHandler(IEmployeeTimeOffRepository repository,
                                              IResourceAuthorizationService auth)
        {
            _repository = repository;
            _auth = auth;
        }

        public async Task<IReadOnlyList<EmployeeTimeOffDto>> Handle(GetEmployeeTimeOffQuery request,
                                                                    CancellationToken cancellationToken)
        {
            await _auth.EnsureCanViewEmployeeAsync(request.EmployeeId, cancellationToken);

            // [From, To] inclusive as days -> half-open wall-clock range.
            var from = request.From.ToDateTime(TimeOnly.MinValue);
            var to = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var blocks = await _repository.GetByEmployeeAndRangeAsync(request.EmployeeId, from, to, cancellationToken);

            return blocks
                .Select(t => new EmployeeTimeOffDto(t.Id, t.EmployeeId, t.Start, t.End, t.Reason))
                .ToList();
        }
    }
}
