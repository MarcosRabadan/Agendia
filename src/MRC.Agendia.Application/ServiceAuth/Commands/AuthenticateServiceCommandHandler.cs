using MediatR;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.ServiceAuth.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Application.ServiceAuth.Commands
{
    /// <summary>
    /// Validates the client credentials and, on success, issues the service token.
    /// Every outcome is audited (client id + result). A failure throws
    /// <see cref="InvalidServiceCredentialsException"/> (mapped to 401); the
    /// authenticator already collapses "unknown", "disabled" and "wrong secret"
    /// into a single result so this handler cannot leak which one occurred.
    /// </summary>
    public class AuthenticateServiceCommandHandler
        : IRequestHandler<AuthenticateServiceCommand, ServiceTokenResponseDto>
    {
        private readonly IServiceClientAuthenticator _authenticator;
        private readonly IServiceTokenIssuer _tokenIssuer;
        private readonly IAuditLogger _auditLogger;

        public AuthenticateServiceCommandHandler(IServiceClientAuthenticator authenticator,
                                                 IServiceTokenIssuer tokenIssuer,
                                                 IAuditLogger auditLogger)
        {
            _authenticator = authenticator;
            _tokenIssuer = tokenIssuer;
            _auditLogger = auditLogger;
        }

        public async Task<ServiceTokenResponseDto> Handle(AuthenticateServiceCommand request,
                                                          CancellationToken cancellationToken)
        {
            var clientId = request.Dto.ClientId;
            var client = _authenticator.Authenticate(clientId, request.Dto.ClientSecret);

            if (client is null)
            {
                await _auditLogger.LogAsync(AuditActions.ServiceTokenDenied,
                                            entityType: "ServiceClient",
                                            entityId: clientId,
                                            cancellationToken: cancellationToken);
                throw new InvalidServiceCredentialsException();
            }

            var token = _tokenIssuer.Issue(client);

            await _auditLogger.LogAsync(AuditActions.ServiceTokenIssued,
                                        entityType: "ServiceClient",
                                        entityId: client.ClientId,
                                        details: new { client.Role },
                                        cancellationToken: cancellationToken);

            return new ServiceTokenResponseDto(token.AccessToken, token.ExpiresAtUtc, "Bearer");
        }
    }
}
