using FluentAssertions;
using Moq;
using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Application.UseCases.Auditoria.Commands;
using OffPay.Domain.Entities;
using OffPay.Domain.Exceptions;

namespace OffPay.Tests.Application.UseCases;

public class ReceberLoteHandlerTests
{
    private readonly Mock<IDispositivoRepository> _dispositivoRepoMock = new();
    private readonly Mock<ILogAuditoriaRepository> _logRepoMock = new();
    private readonly Mock<IPayloadBrutoRepository> _payloadRepoMock = new();
    private readonly Mock<IServicoCripto> _servicoCriptoMock = new();
    private readonly ReceberLoteHandler _handler;

    private const string LoteId = "550e8400-e29b-41d4-a716-446655440000";
    private const string ZeroHash = "0000000000000000000000000000000000000000000000000000000000000000";
    private const string Hash1 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Hash2 = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Hash3 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    public ReceberLoteHandlerTests()
    {
        _handler = new ReceberLoteHandler(
            _dispositivoRepoMock.Object,
            _logRepoMock.Object,
            _payloadRepoMock.Object,
            _servicoCriptoMock.Object);

        // Setup padrão para persistência — não afeta a lógica testada
        _payloadRepoMock
            .Setup(p => p.SalvarPayloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), default))
            .ReturnsAsync("mongo-id");
        _payloadRepoMock
            .Setup(p => p.AtualizarResultadoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), default))
            .Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AdicionarAsync(It.IsAny<LogAuditoria>(), default)).Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AdicionarVariosAsync(It.IsAny<IEnumerable<LogAuditoria>>(), default)).Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.SalvarAlteracoesAsync(default)).Returns(Task.CompletedTask);
    }

    // ── Dispositivo ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_DispositivoNaoEncontrado_LancaExcecao()
    {
        // Arrange
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync("id-inexistente", default))
            .ReturnsAsync((Dispositivo?)null);

        var command = CriarCommand("id-inexistente", CriarTransacao("tx-1", ZeroHash, Hash1));

        // Act & Assert
        await _handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<DispositivoNaoEncontradoException>();
    }

    [Fact]
    public async Task HandleAsync_DispositivoBloqueado_SalvaPayloadCriaLogsELancaExcecao()
    {
        // Arrange
        var dispositivo = CriarDispositivoBloqueado();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var command = CriarCommand(dispositivo.IdentificadorPublico, CriarTransacao("tx-1", ZeroHash, Hash1));

        // Act & Assert
        await _handler.Invoking(h => h.HandleAsync(command))
            .Should().ThrowAsync<DispositivoInativoException>();

        // Payload forense deve ser salvo mesmo com dispositivo bloqueado
        _payloadRepoMock.Verify(p => p.SalvarPayloadAsync(LoteId, dispositivo.IdentificadorPublico, It.IsAny<object>(), default), Times.Once);
        _logRepoMock.Verify(r => r.AdicionarVariosAsync(It.IsAny<IEnumerable<LogAuditoria>>(), default), Times.Once);
    }

    // ── Validação de transações ──────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UmaTransacaoValida_RetornaValidado()
    {
        // Arrange
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var tx = CriarTransacao("tx-1", ZeroHash, Hash1);
        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx.ConteudoCanonico, tx.HashAnterior, tx.Assinatura)).Returns(Hash1);
        _servicoCriptoMock.Setup(s => s.ValidarAssinatura(dispositivo.ChavePublicaPem, tx.ConteudoCanonico, tx.HashAnterior, tx.Assinatura)).Returns(true);

        var command = CriarCommand(dispositivo.IdentificadorPublico, tx);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.TransacoesValidadas.Should().Be(1);
        resultado.TransacoesRejeitadas.Should().Be(0);
        resultado.Resultados.Should().ContainSingle(r => r.TransacaoId == "tx-1" && r.Status == "Validado");
    }

    [Fact]
    public async Task HandleAsync_AssinaturaInvalida_RetornaAssinaturaInvalida()
    {
        // Arrange
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var tx = CriarTransacao("tx-1", ZeroHash, Hash1);
        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx.ConteudoCanonico, tx.HashAnterior, tx.Assinatura)).Returns(Hash1);
        _servicoCriptoMock.Setup(s => s.ValidarAssinatura(dispositivo.ChavePublicaPem, tx.ConteudoCanonico, tx.HashAnterior, tx.Assinatura)).Returns(false);

        var command = CriarCommand(dispositivo.IdentificadorPublico, tx);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.Resultados.Should().ContainSingle(r => r.TransacaoId == "tx-1" && r.Status == "AssinaturaInvalida");
        resultado.TransacoesRejeitadas.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_HashAnteriorErrado_RetornaHashQuebrado()
    {
        // Arrange
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        // HashAnterior deveria ser ZeroHash mas está diferente
        var tx = CriarTransacao("tx-1", Hash2, Hash1);
        var command = CriarCommand(dispositivo.IdentificadorPublico, tx);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.Resultados.Should().ContainSingle(r => r.TransacaoId == "tx-1" && r.Status == "HashQuebrado");
    }

    [Fact]
    public async Task HandleAsync_HashAtualDivergente_RetornaHashQuebrado()
    {
        // Arrange
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var tx = CriarTransacao("tx-1", ZeroHash, Hash1);
        // Servico recalcula e retorna Hash2, mas tx declara Hash1 — divergência
        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx.ConteudoCanonico, tx.HashAnterior, tx.Assinatura)).Returns(Hash2);

        var command = CriarCommand(dispositivo.IdentificadorPublico, tx);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.Resultados.Should().ContainSingle(r => r.TransacaoId == "tx-1" && r.Status == "HashQuebrado");
    }

    // ── Cascata ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_HashQuebradoNaTx2_Tx3RecebeCascata()
    {
        // Arrange
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var tx1 = CriarTransacao("tx-1", ZeroHash, Hash1, DateTime.UtcNow.AddMinutes(-2));
        var tx2 = CriarTransacao("tx-2", ZeroHash, Hash2, DateTime.UtcNow.AddMinutes(-1)); // hashAnterior errado (deveria ser Hash1)
        var tx3 = CriarTransacao("tx-3", Hash2, Hash3, DateTime.UtcNow);

        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx1.ConteudoCanonico, tx1.HashAnterior, tx1.Assinatura)).Returns(Hash1);
        _servicoCriptoMock.Setup(s => s.ValidarAssinatura(dispositivo.ChavePublicaPem, tx1.ConteudoCanonico, tx1.HashAnterior, tx1.Assinatura)).Returns(true);

        var command = CriarCommand(dispositivo.IdentificadorPublico, tx1, tx2, tx3);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.Resultados.Should().HaveCount(3);
        resultado.Resultados.Single(r => r.TransacaoId == "tx-1").Status.Should().Be("Validado");
        resultado.Resultados.Single(r => r.TransacaoId == "tx-2").Status.Should().Be("HashQuebrado");
        resultado.Resultados.Single(r => r.TransacaoId == "tx-3").Status.Should().Be("HashQuebrado"); // cascata
    }

    [Fact]
    public async Task HandleAsync_AssinaturaInvalidaNaTx1_Tx2PodeSerValidada()
    {
        // Arrange — AssinaturaInvalida NÃO cascateia; hashAnteriorEsperado é atualizado normalmente
        var dispositivo = CriarDispositivoAtivo();
        _dispositivoRepoMock
            .Setup(r => r.ObterPorIdentificadorPublicoAsync(dispositivo.IdentificadorPublico, default))
            .ReturnsAsync(dispositivo);

        var tx1 = CriarTransacao("tx-1", ZeroHash, Hash1, DateTime.UtcNow.AddMinutes(-1));
        var tx2 = CriarTransacao("tx-2", Hash1, Hash2, DateTime.UtcNow); // hashAnterior = Hash1 (hashAtual da tx1)

        // tx1: hash confere, mas assinatura inválida
        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx1.ConteudoCanonico, tx1.HashAnterior, tx1.Assinatura)).Returns(Hash1);
        _servicoCriptoMock.Setup(s => s.ValidarAssinatura(dispositivo.ChavePublicaPem, tx1.ConteudoCanonico, tx1.HashAnterior, tx1.Assinatura)).Returns(false);

        // tx2: hash confere, assinatura válida
        _servicoCriptoMock.Setup(s => s.CalcularHashTransacao(tx2.ConteudoCanonico, tx2.HashAnterior, tx2.Assinatura)).Returns(Hash2);
        _servicoCriptoMock.Setup(s => s.ValidarAssinatura(dispositivo.ChavePublicaPem, tx2.ConteudoCanonico, tx2.HashAnterior, tx2.Assinatura)).Returns(true);

        var command = CriarCommand(dispositivo.IdentificadorPublico, tx1, tx2);

        // Act
        var resultado = await _handler.HandleAsync(command);

        // Assert
        resultado.Resultados.Single(r => r.TransacaoId == "tx-1").Status.Should().Be("AssinaturaInvalida");
        resultado.Resultados.Single(r => r.TransacaoId == "tx-2").Status.Should().Be("Validado");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Dispositivo CriarDispositivoAtivo()
        => Dispositivo.Criar("Caixa 01", "comerciante-1", "-----BEGIN PUBLIC KEY-----\nMFkw\n-----END PUBLIC KEY-----");

    private static Dispositivo CriarDispositivoBloqueado()
    {
        var d = Dispositivo.Criar("Caixa 01", "comerciante-1", "pem");
        d.Bloquear("motivo de teste");
        return d;
    }

    private static TransacaoAuditoriaDto CriarTransacao(
        string id, string hashAnterior, string hashAtual, DateTime? timestamp = null)
        => new(id, timestamp ?? DateTime.UtcNow, $"{{\"id\":\"{id}\"}}", hashAnterior, hashAtual, $"assinatura-{id}");

    private static ReceberLoteCommand CriarCommand(string dispositivoId, params TransacaoAuditoriaDto[] transacoes)
        => new(LoteId, dispositivoId, DateTime.UtcNow, transacoes);
}
