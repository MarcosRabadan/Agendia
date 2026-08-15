using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Business.Commands.Create;
using MRC.Agendia.Application.Business.Commands.Delete;
using MRC.Agendia.Application.Business.Commands.Restore;
using MRC.Agendia.Application.Business.Commands.Update;
using MRC.Agendia.Application.Business.DTO;
using NSubstitute;
using IBusinessService = MRC.Agendia.Application.Business.IBusinessService;

namespace MRC.Agendia.Tests.Unit.Application.CrudHandlers
{
    /// <summary>
    /// Unit tests for the Business CRUD handlers. The handlers are thin, but they
    /// own one security-critical contract: an Update must authorize the EXISTING
    /// business (by its own id) BEFORE mutating, so a failed check never reaches the
    /// service.
    /// </summary>
    public class BusinessHandlersTests
    {
        private readonly IBusinessService _service = Substitute.For<IBusinessService>();
        private readonly IResourceAuthorizationService _auth = Substitute.For<IResourceAuthorizationService>();

        private static UpdateBusinessDto UpdateDto(Guid? id = null) => new(id ?? TestIds.Of(5), IsActive: true);

        private static BusinessDto Result(Guid? id = null) => new(id ?? TestIds.Of(5), IsActive: true);

        [Fact]
        public async Task Update_authorizes_the_existing_business_then_delegates()
        {
            var expected = Result();
            _service.UpdateAsync(Arg.Any<UpdateBusinessDto>(), Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new UpdateBusinessCommandHandler(_service, _auth)
                .Handle(new UpdateBusinessCommand(UpdateDto(TestIds.Of(5))), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageBusinessAsync(TestIds.Of(5), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_does_not_touch_the_service_when_authorization_fails()
        {
            _auth.EnsureCanManageBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns<Task>(_ => throw new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new UpdateBusinessCommandHandler(_service, _auth)
                    .Handle(new UpdateBusinessCommand(UpdateDto()), default));

            await _service.DidNotReceive().UpdateAsync(Arg.Any<UpdateBusinessDto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Create_delegates_to_the_service()
        {
            var dto = new CreateBusinessDto("owner-1");
            var expected = Result();
            _service.CreateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateBusinessCommandHandler(_service)
                .Handle(new CreateBusinessCommand(dto), default);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task Delete_and_Restore_delegate_with_the_id()
        {
            _service.DeleteAsync(TestIds.Of(9), Arg.Any<CancellationToken>()).Returns(true);
            _service.RestoreAsync(TestIds.Of(9), Arg.Any<CancellationToken>()).Returns(true);

            Assert.True(await new DeleteBusinessCommandHandler(_service).Handle(new DeleteBusinessCommand(TestIds.Of(9)), default));
            Assert.True(await new RestoreBusinessCommandHandler(_service).Handle(new RestoreBusinessCommand(TestIds.Of(9)), default));
        }
    }
}
