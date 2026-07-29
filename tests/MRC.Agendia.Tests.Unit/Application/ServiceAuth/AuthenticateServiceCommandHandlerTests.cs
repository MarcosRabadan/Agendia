using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.ServiceAuth;
using MRC.Agendia.Application.ServiceAuth.Commands;
using MRC.Agendia.Application.ServiceAuth.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Exceptions;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.ServiceAuth
{
    /// <summary>
    /// Unit tests for the service-authentication handler: on valid credentials it
    /// issues a token and audits success; on failure it throws
    /// <see cref="InvalidServiceCredentialsException"/> and audits the denial
    /// without issuing anything.
    /// </summary>
    public class AuthenticateServiceCommandHandlerTests
    {
        private readonly IServiceClientAuthenticator _authenticator = Substitute.For<IServiceClientAuthenticator>();
        private readonly IServiceTokenIssuer _tokenIssuer = Substitute.For<IServiceTokenIssuer>();
        private readonly IAuditLogger _auditLogger = Substitute.For<IAuditLogger>();

        private AuthenticateServiceCommandHandler Handler() => new(_authenticator, _tokenIssuer, _auditLogger);

        private static AuthenticateServiceCommand Command(string clientId = "soundmate", string secret = "secret") =>
            new(new ServiceTokenRequestDto(clientId, secret));

        [Fact]
        public async Task Valid_credentials_issue_a_token_and_audit_success()
        {
            var client = new AuthenticatedServiceClient("soundmate", Roles.Admin);
            var expiry = DateTime.UtcNow.AddMinutes(15);
            _authenticator.Authenticate("soundmate", "secret").Returns(client);
            _tokenIssuer.Issue(client).Returns(new ServiceToken("signed.jwt.token", expiry));

            var result = await Handler().Handle(Command(), default);

            Assert.Equal("signed.jwt.token", result.AccessToken);
            Assert.Equal(expiry, result.ExpiresAt);
            Assert.Equal("Bearer", result.TokenType);

            await _auditLogger.Received(1).LogAsync(
                AuditActions.ServiceTokenIssued,
                "ServiceClient",
                "soundmate",
                Arg.Any<object>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Invalid_credentials_throw_and_audit_denial_without_issuing()
        {
            _authenticator.Authenticate(Arg.Any<string>(), Arg.Any<string>())
                .Returns((AuthenticatedServiceClient?)null);

            await Assert.ThrowsAsync<InvalidServiceCredentialsException>(() =>
                Handler().Handle(Command(clientId: "intruder"), default));

            _tokenIssuer.DidNotReceive().Issue(Arg.Any<AuthenticatedServiceClient>());
            await _auditLogger.Received(1).LogAsync(
                AuditActions.ServiceTokenDenied,
                "ServiceClient",
                "intruder",
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>());
        }
    }
}
