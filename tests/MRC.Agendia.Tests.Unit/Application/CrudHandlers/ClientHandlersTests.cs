using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Clients;
using MRC.Agendia.Application.Clients.Commands.Create;
using MRC.Agendia.Application.Clients.Commands.Delete;
using MRC.Agendia.Application.Clients.Commands.Restore;
using MRC.Agendia.Application.Clients.Commands.Update;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Clients.Queries.GetAll;
using MRC.Agendia.Application.Clients.Queries.GetByBusiness;
using MRC.Agendia.Application.Clients.Queries.GetById;
using MRC.Agendia.Application.Common;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.CrudHandlers
{
    /// <summary>
    /// Unit tests for the Client CRUD handlers. Business-scoped operations authorize
    /// the owning business; per-client operations authorize the client. The global
    /// self-registration create carries no handler authorization on purpose (it is
    /// open, gated only by input validation). Each authorized handler must not reach
    /// the service when the check fails.
    /// </summary>
    public class ClientHandlersTests
    {
        private readonly IClientService _service = Substitute.For<IClientService>();
        private readonly IResourceAuthorizationService _auth = Substitute.For<IResourceAuthorizationService>();

        private static ClientDto Result(int id = 1, int? businessId = null) =>
            new(id, "Ana", "600", "a@b.c", businessId);

        [Fact]
        public async Task CreateForBusiness_authorizes_the_business_then_delegates()
        {
            var dto = new CreateClientDto("Ana", "600", "a@b.c");
            var expected = Result(businessId: 7);
            _service.CreateForBusinessAsync(7, dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateBusinessClientCommandHandler(_service, _auth)
                .Handle(new CreateBusinessClientCommand(7, dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageBusinessResourcesAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GlobalCreate_delegates_without_handler_authorization()
        {
            var dto = new CreateClientDto("Ana", "600", "a@b.c");
            var expected = Result();
            _service.CreateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new CreateClientCommandHandler(_service)
                .Handle(new CreateClientCommand(dto), default);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task Update_authorizes_the_client_then_delegates()
        {
            var dto = new UpdateClientDto(5, "Ana", "600", "a@b.c");
            var expected = Result(5);
            _service.UpdateAsync(dto, Arg.Any<CancellationToken>()).Returns(expected);

            var result = await new UpdateClientCommandHandler(_service, _auth)
                .Handle(new UpdateClientCommand(dto), default);

            Assert.Same(expected, result);
            await _auth.Received(1).EnsureCanManageClientAsync(5, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_and_Delete_do_not_touch_the_service_when_authorization_fails()
        {
            _auth.EnsureCanManageClientAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Task>(_ => throw new UnauthorizedAccessException());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new UpdateClientCommandHandler(_service, _auth)
                    .Handle(new UpdateClientCommand(new UpdateClientDto(5, "Ana", "600", null)), default));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                new DeleteClientCommandHandler(_service, _auth)
                    .Handle(new DeleteClientCommand(5), default));

            await _service.DidNotReceive().UpdateAsync(Arg.Any<UpdateClientDto>(), Arg.Any<CancellationToken>());
            await _service.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetById_and_GetByBusiness_authorize_then_delegate()
        {
            var one = Result(5);
            _service.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(one);
            var page = PagedResult<ClientDto>.Create(Array.Empty<ClientDto>(), 0, 1, 50);
            _service.GetPagedByBusinessAsync(7, 1, 50, Arg.Any<CancellationToken>()).Returns(page);

            Assert.Same(one, await new GetClientByIdQueryHandler(_service, _auth).Handle(new GetClientByIdQuery(5), default));
            Assert.Same(page, await new GetBusinessClientsQueryHandler(_service, _auth)
                .Handle(new GetBusinessClientsQuery(7, 1, 50), default));

            await _auth.Received(1).EnsureCanManageClientAsync(5, Arg.Any<CancellationToken>());
            await _auth.Received(1).EnsureCanManageBusinessResourcesAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GlobalGetAll_and_Restore_delegate()
        {
            var page = PagedResult<ClientDto>.Create(Array.Empty<ClientDto>(), 0, 1, 50);
            _service.GetPagedAsync(1, 50, Arg.Any<CancellationToken>()).Returns(page);
            _service.RestoreAsync(5, Arg.Any<CancellationToken>()).Returns(true);

            Assert.Same(page, await new GetAllClientsQueryHandler(_service).Handle(new GetAllClientsQuery(1, 50), default));
            Assert.True(await new RestoreClientCommandHandler(_service).Handle(new RestoreClientCommand(5), default));
        }
    }
}
