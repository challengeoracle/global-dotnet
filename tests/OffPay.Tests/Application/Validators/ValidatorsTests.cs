using FluentAssertions;
using OffPay.Application.DTOs;
using OffPay.Application.Validators;

namespace OffPay.Tests.Application.Validators;

public class RegistrarDispositivoRequestValidatorTests
{
    private readonly RegistrarDispositivoRequestValidator _validator = new();
    private const string PemValido = "-----BEGIN PUBLIC KEY-----\nMFkw\n-----END PUBLIC KEY-----";

    [Fact]
    public void Validate_NomeVazio_RetornaErro()
    {
        var result = _validator.Validate(new RegistrarDispositivoRequest("", "comerciante-1", PemValido));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public void Validate_NomeMuitoLongo_RetornaErro()
    {
        var result = _validator.Validate(new RegistrarDispositivoRequest(new string('x', 121), "comerciante-1", PemValido));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nome");
    }

    [Fact]
    public void Validate_ComercianteIdVazio_RetornaErro()
    {
        var result = _validator.Validate(new RegistrarDispositivoRequest("Caixa 01", "", PemValido));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ComercianteId");
    }

    [Fact]
    public void Validate_ChavePublicaSemCabecalhoPem_RetornaErro()
    {
        var result = _validator.Validate(new RegistrarDispositivoRequest("Caixa 01", "comerciante-1", "chave-invalida"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ChavePublicaPem");
    }

    [Fact]
    public void Validate_RequestValida_SemErros()
    {
        var result = _validator.Validate(new RegistrarDispositivoRequest("Caixa 01", "comerciante-1", PemValido));
        result.IsValid.Should().BeTrue();
    }
}

public class BloquearDispositivoRequestValidatorTests
{
    private readonly BloquearDispositivoRequestValidator _validator = new();

    [Fact]
    public void Validate_MotivoVazio_RetornaErro()
    {
        var result = _validator.Validate(new BloquearDispositivoRequest(""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_MotivoMuitoLongo_RetornaErro()
    {
        var result = _validator.Validate(new BloquearDispositivoRequest(new string('x', 501)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validate_MotivoValido_SemErros()
    {
        var result = _validator.Validate(new BloquearDispositivoRequest("Suspeita de fraude."));
        result.IsValid.Should().BeTrue();
    }
}

public class LoteAuditoriaRequestValidatorTests
{
    private readonly LoteAuditoriaRequestValidator _validator = new();

    private static readonly string HashValido = new string('a', 64);
    private static readonly string ZeroHash = new string('0', 64);

    [Fact]
    public void Validate_LoteIdVazio_RetornaErro()
    {
        var request = CriarLoteValido() with { LoteId = "" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LoteId");
    }

    [Fact]
    public void Validate_DispositivoIdentificadorVazio_RetornaErro()
    {
        var request = CriarLoteValido() with { DispositivoIdentificador = "" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DispositivoIdentificador");
    }

    [Fact]
    public void Validate_TransacoesVazias_RetornaErro()
    {
        var request = CriarLoteValido() with { Transacoes = Array.Empty<TransacaoAuditoriaDto>() };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Transacoes");
    }

    [Fact]
    public void Validate_HashAnteriorComMenosDe64Chars_RetornaErro()
    {
        var tx = new TransacaoAuditoriaDto("tx-1", DateTime.UtcNow, "{}", new string('a', 32), HashValido, "sig");
        var request = CriarLoteValido() with { Transacoes = new[] { tx } };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HashAnteriorComLetrasUppercase_RetornaErro()
    {
        var tx = new TransacaoAuditoriaDto("tx-1", DateTime.UtcNow, "{}", new string('A', 64), HashValido, "sig");
        var request = CriarLoteValido() with { Transacoes = new[] { tx } };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HashAtualComCaracteresInvalidos_RetornaErro()
    {
        var tx = new TransacaoAuditoriaDto("tx-1", DateTime.UtcNow, "{}", ZeroHash, new string('g', 64), "sig");
        var request = CriarLoteValido() with { Transacoes = new[] { tx } };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RequestComUmaTransacaoValida_SemErros()
    {
        var result = _validator.Validate(CriarLoteValido());
        result.IsValid.Should().BeTrue();
    }

    private static LoteAuditoriaRequest CriarLoteValido()
    {
        var transacao = new TransacaoAuditoriaDto(
            "660e8400-e29b-41d4-a716-446655440001",
            DateTime.UtcNow,
            "{\"valor\":100.00}",
            ZeroHash,
            HashValido,
            "assinatura-base64");

        return new LoteAuditoriaRequest(
            "550e8400-e29b-41d4-a716-446655440000",
            "identificador-publico-uuid",
            DateTime.UtcNow,
            new[] { transacao });
    }
}
