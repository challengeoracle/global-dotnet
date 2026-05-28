using FluentAssertions;
using Moq;
using OffPay.Application.Abstractions;
using OffPay.Application.UseCases.Dispositivos.Commands;
using OffPay.Domain.Entities;
using OffPay.Domain.Enums;
using OffPay.Domain.Exceptions;

namespace OffPay.Tests.Application.UseCases;

public class RegistrarDispositivoHandlerTests
{
    private readonly Mock<IDispositivoRepository> _repoMock = new();
    private readonly Mock<IDeviceTokenService> _deviceTokenMock = new();
    private readonly RegistrarDispositivoHandler _handler;

    public RegistrarDispositivoHandlerTests()
    {
        _handler = new RegistrarDispositivoHandler(_repoMock.Object, _deviceTokenMock.Object);
    }

    [Fact]
    public async Task HandleAsync_DadosValidos_CriaDispositivoERetornaDeviceToken()
    {
        // Arrange
        const string deviceToken = "jwt-device-token-gerado";
        _repoMock.Setup(r => r.AdicionarAsync(It.IsAny<Dispositivo>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SalvarAlteracoesAsync(default)).Returns(Task.CompletedTask);
        _deviceTokenMock.Setup(d => d.GerarDeviceToken(It.IsAny<string>())).Returns(deviceToken);

        var command = new RegistrarDispositivoCommand(
            "Caixa 01", "comerciante-uuid", "-----BEGIN PUBLIC KEY-----\nMFkw\n-----END PUBLIC KEY-----");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.DeviceToken.Should().Be(deviceToken);
        result.IdentificadorPublico.Should().NotBeNullOrEmpty();
        _repoMock.Verify(r => r.AdicionarAsync(It.IsAny<Dispositivo>(), default), Times.Once);
        _repoMock.Verify(r => r.SalvarAlteracoesAsync(default), Times.Once);
    }
}

public class BloquearDispositivoHandlerTests
{
    private readonly Mock<IDispositivoRepository> _repoMock = new();
    private readonly BloquearDispositivoHandler _handler;

    public BloquearDispositivoHandlerTests()
    {
        _handler = new BloquearDispositivoHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_DispositivoNaoEncontrado_LancaExcecao()
    {
        // Arrange
        _repoMock.Setup(r => r.ObterPorIdentificadorPublicoAsync("id-inexistente", default))
            .ReturnsAsync((Dispositivo?)null);

        // Act
        var act = () => _handler.HandleAsync(new BloquearDispositivoCommand("id-inexistente", "motivo"));

        // Assert
        await act.Should().ThrowAsync<DispositivoNaoEncontradoException>();
    }

    [Fact]
    public async Task HandleAsync_DispositivoAtivo_AlteraStatusParaBloqueadoESalva()
    {
        // Arrange
        var dispositivo = Dispositivo.Criar("Caixa 01", "comerciante-1", "pem");
        _repoMock.Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);
        _repoMock.Setup(r => r.SalvarAlteracoesAsync(default)).Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(new BloquearDispositivoCommand(dispositivo.IdentificadorPublico, "suspeita de fraude"));

        // Assert
        dispositivo.Status.Should().Be(StatusDispositivo.Bloqueado);
        dispositivo.MotivoBloqueio.Should().Be("suspeita de fraude");
        _repoMock.Verify(r => r.SalvarAlteracoesAsync(default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DispositivoRevogado_LancaDomainException()
    {
        // Arrange
        var dispositivo = Dispositivo.Criar("Caixa 01", "comerciante-1", "pem");
        dispositivo.RevogarChaves();
        _repoMock.Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        // Act
        var act = () => _handler.HandleAsync(new BloquearDispositivoCommand(dispositivo.IdentificadorPublico, "motivo"));

        // Assert — Dispositivo.Bloquear() lança DomainException quando status é Revogado
        await act.Should().ThrowAsync<DomainException>();
    }
}

public class RevogarChavesDispositivoHandlerTests
{
    private readonly Mock<IDispositivoRepository> _repoMock = new();
    private readonly RevogarChavesDispositivoHandler _handler;

    public RevogarChavesDispositivoHandlerTests()
    {
        _handler = new RevogarChavesDispositivoHandler(_repoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_DispositivoNaoEncontrado_LancaExcecao()
    {
        // Arrange
        _repoMock.Setup(r => r.ObterPorIdentificadorPublicoAsync("id-inexistente", default))
            .ReturnsAsync((Dispositivo?)null);

        // Act
        var act = () => _handler.HandleAsync(new RevogarChavesDispositivoCommand("id-inexistente"));

        // Assert
        await act.Should().ThrowAsync<DispositivoNaoEncontradoException>();
    }

    [Fact]
    public async Task HandleAsync_DispositivoAtivo_AlteraStatusParaRevogadoESalva()
    {
        // Arrange
        var dispositivo = Dispositivo.Criar("Caixa 01", "comerciante-1", "pem");
        _repoMock.Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);
        _repoMock.Setup(r => r.SalvarAlteracoesAsync(default)).Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(new RevogarChavesDispositivoCommand(dispositivo.IdentificadorPublico));

        // Assert
        dispositivo.Status.Should().Be(StatusDispositivo.Revogado);
        _repoMock.Verify(r => r.SalvarAlteracoesAsync(default), Times.Once);
    }
}
