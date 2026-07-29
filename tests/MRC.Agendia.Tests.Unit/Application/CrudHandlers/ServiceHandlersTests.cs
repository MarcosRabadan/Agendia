using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services;
using MRC.Agendia.Application.Services.Commands.Create;
using MRC.Agendia.Application.Services.Commands.Delete;
using MRC.Agendia.Application.Services.Commands.Restore;
using MRC.Agendia.Application.Services.Commands.Update;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Application.Services.Queries.GetAll;
using MRC.Agendia.Application.Services.Queries.GetById;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.CrudHandlers
{
    /// <summary>
    /// Unit tests for the Service CRUD handlers. Create authorizes the TARGET
    /// business (a service cannot be created under a business the caller cannot
    /// manage); Update/Delete authorize the EXISTING service (resolving its business
    /// so it cannot be relocated cross-tenant). Each check must run before the
    /// service is touched.
    /// </summary>
    public class ServiceHandlersTests
    {
        private readonly IServicesService _service = Substitute.For<IServicesService>();
        private readonly IResourceAuthorizationService _auth = Substitute.For<IResourceAuthorizationService>();

        private static ServiceDto Result(int id = 1, int businessId = 7) =>
            new(id, businessId, "Corte", null, 30, 15m);

        [Fact]
        public async Task Create_authorizes_the_target_business_then_delegates()
        {
            var dto = new CreateServiceDto(BusinessId: 7, Name: "Corte", Description: null, DurationMinutes: 30, Price: 15m);
            var expected = Result();
            _service.CreateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateServiceCommandHandler(_service, _auth)
                .Handle(new CreateServiceCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageBusinessResourcesAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_authorizes_the_existing_service_then_delegates()
        {
            var dto = new UpdateServiceDto(1, "Corte", null, 30, 15m);
            var expected = Result();
            _service.UpdateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new UpdateServiceCommandHandler(_service, _auth)
                .Handle(new UpdateServiceCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageServiceAsync(1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_does_not_touch_the_service_when_authorization_fails()
        {
            _auth.EnsureCanManageServiceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Task>(_ => throw new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new UpdateServiceCommandHandler(_service, _auth)
                    .Handle(new UpdateServiceCommand(new UpdateServiceDto(1, "X", null, 30, 10m)), default));

            await _service.DidNotReceive().UpdateAsync(Arg.Any<UpdateServiceDto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Delete_authorizes_before_deleting()
        {
            _service.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(true);

            var result = await new DeleteServiceCommandHandler(_service, _auth)
                .Handle(new DeleteServiceCommand(4), default);

            Assert.True(result);
            await _auth.Received(1).EnsureCanManageServiceAsync(4, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Restore_GetAll_GetById_delegate()
        {
            _service.RestoreAsync(4, Arg.Any<CancellationToken>()).Returns(true);
            var page = PagedResult<ServiceDto>.Create(Array.Empty<ServiceDto>(), 0, 1, 50);
            _service.GetPagedAsync(1, 50, Arg.Any<CancellationToken>()).Returns(page);
            var one = Result();
            _service.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(one);

            Assert.True(await new RestoreServiceCommandHandler(_service).Handle(new RestoreServiceCommand(4), default));
            Assert.Same(page, await new GetAllServicesQueryHandler(_service).Handle(new GetAllServicesQuery(1, 50), default));
            Assert.Same(one, await new GetServiceByIdQueryHandler(_service).Handle(new GetServiceByIdQuery(1), default));
        }
    }
}
