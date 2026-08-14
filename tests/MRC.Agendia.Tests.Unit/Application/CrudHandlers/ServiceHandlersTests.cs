using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Services;
using MRC.Agendia.Application.Services.Commands.Create;
using MRC.Agendia.Application.Services.Commands.Delete;
using MRC.Agendia.Application.Services.Commands.Restore;
using MRC.Agendia.Application.Services.Commands.Update;
using MRC.Agendia.Application.Services.DTO;
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

        private static ServiceDto Result(Guid? id = null, Guid? businessId = null) =>
            new(id ?? TestIds.Of(1), businessId ?? TestIds.Of(7), DurationMinutes: 30);

        [Fact]
        public async Task Create_authorizes_the_target_business_then_delegates()
        {
            var dto = new CreateServiceDto(BusinessId: TestIds.Of(7), DurationMinutes: 30);
            var expected = Result();
            _service.CreateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateServiceCommandHandler(_service, _auth)
                .Handle(new CreateServiceCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageBusinessResourcesAsync(TestIds.Of(7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_authorizes_the_existing_service_then_delegates()
        {
            var dto = new UpdateServiceDto(TestIds.Of(1), 30);
            var expected = Result();
            _service.UpdateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new UpdateServiceCommandHandler(_service, _auth)
                .Handle(new UpdateServiceCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageServiceAsync(TestIds.Of(1), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_does_not_touch_the_service_when_authorization_fails()
        {
            _auth.EnsureCanManageServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns<Task>(_ => throw new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new UpdateServiceCommandHandler(_service, _auth)
                    .Handle(new UpdateServiceCommand(new UpdateServiceDto(TestIds.Of(1), 30)), default));

            await _service.DidNotReceive().UpdateAsync(Arg.Any<UpdateServiceDto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Delete_authorizes_before_deleting()
        {
            _service.DeleteAsync(TestIds.Of(4), Arg.Any<CancellationToken>()).Returns(true);

            var result = await new DeleteServiceCommandHandler(_service, _auth)
                .Handle(new DeleteServiceCommand(TestIds.Of(4)), default);

            Assert.True(result);
            await _auth.Received(1).EnsureCanManageServiceAsync(TestIds.Of(4), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Restore_delegates()
        {
            _service.RestoreAsync(TestIds.Of(4), Arg.Any<CancellationToken>()).Returns(true);

            Assert.True(await new RestoreServiceCommandHandler(_service).Handle(new RestoreServiceCommand(TestIds.Of(4)), default));
        }
    }
}
