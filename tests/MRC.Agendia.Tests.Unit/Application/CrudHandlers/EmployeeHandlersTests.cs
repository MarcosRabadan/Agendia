using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees;
using MRC.Agendia.Application.Employees.Commands.Create;
using MRC.Agendia.Application.Employees.Commands.Delete;
using MRC.Agendia.Application.Employees.Commands.Update;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Employees.Queries.GetAll;
using MRC.Agendia.Application.Employees.Queries.GetById;
using MRC.Agendia.Domain.Constants;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.CrudHandlers
{
    /// <summary>
    /// Unit tests for the Employee CRUD handlers. Create authorizes the target
    /// business; Update/Delete/GetById authorize the existing employee. The list
    /// handler carries real branch logic (Admin sees everything, Owner only their
    /// own businesses, anyone else is rejected) which is defense in depth over the
    /// controller's role gate.
    /// </summary>
    public class EmployeeHandlersTests
    {
        private readonly IEmployeeService _service = Substitute.For<IEmployeeService>();
        private readonly IResourceAuthorizationService _auth = Substitute.For<IResourceAuthorizationService>();
        private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();

        private static EmployeeDto Result(int id = 1, int businessId = 7) =>
            new(id, businessId, IsActive: true, MaxConcurrentAppointments: 1);

        private static PagedResult<EmployeeDto> EmptyPage() =>
            PagedResult<EmployeeDto>.Create(Array.Empty<EmployeeDto>(), 0, 1, 50);

        [Fact]
        public async Task Create_authorizes_the_target_business_then_delegates()
        {
            var dto = new CreateEmployeeDto(BusinessId: 7);
            var expected = Result();
            _service.CreateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateEmployeeCommandHandler(_service, _auth)
                .Handle(new CreateEmployeeCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageBusinessResourcesAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_authorizes_the_existing_employee_then_delegates()
        {
            var dto = new UpdateEmployeeDto(5, IsActive: true, MaxConcurrentAppointments: 2);
            var expected = Result(5);
            _service.UpdateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new UpdateEmployeeCommandHandler(_service, _auth)
                .Handle(new UpdateEmployeeCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanUpdateEmployeeAsync(5, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Delete_does_not_touch_the_service_when_authorization_fails()
        {
            _auth.EnsureCanDeleteEmployeeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Task>(_ => throw new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new DeleteEmployeeCommandHandler(_service, _auth)
                    .Handle(new DeleteEmployeeCommand(5), default));

            await _service.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetById_authorizes_the_view_then_delegates()
        {
            var one = Result(5);
            _service.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(one);

            var result = await new GetEmployeeByIdQueryHandler(_service, _auth).Handle(new GetEmployeeByIdQuery(5), default);

            Assert.Same(one, result);
            await _auth.Received(1).EnsureCanViewEmployeeAsync(5, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAll_as_Admin_returns_every_employee()
        {
            _currentUser.IsInRole(Roles.Admin).Returns(true);
            var page = EmptyPage();
            _service.GetPagedAsync(1, 50, Arg.Any<CancellationToken>()).Returns(page);

            var result = await new GetAllEmployeesQueryHandler(_service, _currentUser)
                .Handle(new GetAllEmployeesQuery(1, 50), default);

            Assert.Same(page, result);
            await _service.DidNotReceive().GetPagedByOwnerUserIdAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAll_as_Owner_scopes_to_their_own_businesses()
        {
            _currentUser.IsInRole(Roles.Admin).Returns(false);
            _currentUser.IsInRole(Roles.BusinessOwner).Returns(true);
            _currentUser.UserId.Returns("owner-1");
            var page = EmptyPage();
            _service.GetPagedByOwnerUserIdAsync("owner-1", 1, 50, Arg.Any<CancellationToken>()).Returns(page);

            var result = await new GetAllEmployeesQueryHandler(_service, _currentUser)
                .Handle(new GetAllEmployeesQuery(1, 50), default);

            Assert.Same(page, result);
            await _service.DidNotReceive().GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAll_as_Owner_without_a_user_id_is_rejected()
        {
            _currentUser.IsInRole(Roles.BusinessOwner).Returns(true);
            _currentUser.UserId.Returns((string?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new GetAllEmployeesQueryHandler(_service, _currentUser)
                    .Handle(new GetAllEmployeesQuery(1, 50), default));
        }

        [Fact]
        public async Task GetAll_as_any_other_role_is_rejected()
        {
            _currentUser.IsInRole(Arg.Any<string>()).Returns(false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new GetAllEmployeesQueryHandler(_service, _currentUser)
                    .Handle(new GetAllEmployeesQuery(1, 50), default));
        }
    }
}
