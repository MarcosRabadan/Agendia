using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.DeviceTokens.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;
using MRC.Agendia.Application.DeviceTokens.Commands.Register;
using MRC.Agendia.Application.DeviceTokens.Commands.Remove;

namespace MRC.Agendia.Tests.Unit.Application.DeviceTokens
{
    /// <summary>
    /// Unit tests for the device-token register/remove handlers (#51): register is
    /// an idempotent upsert that re-points a token at the caller, and remove only
    /// deletes a token that belongs to the caller.
    /// </summary>
    public class DeviceTokenCommandHandlerTests
    {
        private const string UserId = "user-1";

        private readonly IDeviceTokenRepository _repository = Substitute.For<IDeviceTokenRepository>();
        private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        public DeviceTokenCommandHandlerTests()
        {
            _currentUser.IsAuthenticated.Returns(true);
            _currentUser.UserId.Returns(UserId);
        }

        private RegisterDeviceTokenCommandHandler RegisterHandler() => new(_repository, _currentUser, _unitOfWork);
        private RemoveDeviceTokenCommandHandler RemoveHandler() => new(_repository, _currentUser, _unitOfWork);

        [Fact]
        public async Task Register_TokenNuevo_LoAnade()
        {
            _repository.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((DeviceToken?)null);

            await RegisterHandler().Handle(
                new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("tok", DevicePlatform.Android)), default);

            await _repository.Received(1).AddAsync(
                Arg.Is<DeviceToken>(d => d.Token == "tok" && d.UserId == UserId && d.Platform == DevicePlatform.Android),
                Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Register_TokenExistente_LoReasignaAlUsuarioActual()
        {
            var existing = new DeviceToken { Id = 9, Token = "tok", UserId = "otro-usuario", Platform = DevicePlatform.Web };
            _repository.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(existing);

            await RegisterHandler().Handle(
                new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("tok", DevicePlatform.Ios)), default);

            Assert.Equal(UserId, existing.UserId);
            Assert.Equal(DevicePlatform.Ios, existing.Platform);
            _repository.Received(1).Update(existing);
            await _repository.DidNotReceive().AddAsync(Arg.Any<DeviceToken>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Remove_TokenPropio_LoBorra()
        {
            var existing = new DeviceToken { Id = 1, Token = "tok", UserId = UserId, Platform = DevicePlatform.Android };
            _repository.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(existing);

            await RemoveHandler().Handle(new RemoveDeviceTokenCommand(new RemoveDeviceTokenDto("tok")), default);

            _repository.Received(1).Delete(existing);
            await _unitOfWork.Received(1).Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Remove_TokenDeOtroUsuario_NoLoBorra()
        {
            var existing = new DeviceToken { Id = 1, Token = "tok", UserId = "otro-usuario", Platform = DevicePlatform.Android };
            _repository.GetByTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(existing);

            await RemoveHandler().Handle(new RemoveDeviceTokenCommand(new RemoveDeviceTokenDto("tok")), default);

            _repository.DidNotReceive().Delete(Arg.Any<DeviceToken>());
            await _unitOfWork.DidNotReceive().Save(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Register_SinAutenticar_Lanza()
        {
            _currentUser.IsAuthenticated.Returns(false);
            _currentUser.UserId.Returns((string?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                RegisterHandler().Handle(
                    new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("tok", DevicePlatform.Android)), default));
        }
    }
}
