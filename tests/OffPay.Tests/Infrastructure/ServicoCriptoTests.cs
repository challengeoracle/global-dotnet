using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using OffPay.Infrastructure.Cripto;

namespace OffPay.Tests.Infrastructure;

public class ServicoCriptoTests
{
    private readonly ServicoCripto _servico = new();

    private static (string chavePublicaPem, byte[] chavePrivadaBytes) GerarParDeChaves()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (ecdsa.ExportSubjectPublicKeyInfoPem(), ecdsa.ExportECPrivateKey());
    }

    private static string AssinarDer(byte[] chavePrivadaBytes, string conteudoCanonico, string hashAnterior)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ecdsa.ImportECPrivateKey(chavePrivadaBytes, out _);
        var dados = Encoding.UTF8.GetBytes(conteudoCanonico + hashAnterior);
        var assinaturaBytes = ecdsa.SignData(dados, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return Convert.ToBase64String(assinaturaBytes);
    }

    // ── Validação de assinatura ──────────────────────────────────────────────

    [Fact]
    public void ValidarAssinatura_AssinaturaCorreta_RetornaTrue()
    {
        var (pubPem, privBytes) = GerarParDeChaves();
        var conteudo = "{\"valor\":150.00}";
        var hashAnterior = new string('0', 64);

        var assinatura = AssinarDer(privBytes, conteudo, hashAnterior);

        _servico.ValidarAssinatura(pubPem, conteudo, hashAnterior, assinatura).Should().BeTrue();
    }

    [Fact]
    public void ValidarAssinatura_ConteudoAlterado_RetornaFalse()
    {
        var (pubPem, privBytes) = GerarParDeChaves();
        var conteudo = "{\"valor\":150.00}";
        var hashAnterior = new string('0', 64);

        // Assina o conteúdo original, mas valida com conteúdo adulterado
        var assinatura = AssinarDer(privBytes, conteudo, hashAnterior);

        _servico.ValidarAssinatura(pubPem, "{\"valor\":999.00}", hashAnterior, assinatura).Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_HashAnteriorAlterado_RetornaFalse()
    {
        var (pubPem, privBytes) = GerarParDeChaves();
        var conteudo = "{\"valor\":50.00}";
        var hashAnterior = new string('0', 64);

        var assinatura = AssinarDer(privBytes, conteudo, hashAnterior);
        var hashFalso = new string('f', 64);

        _servico.ValidarAssinatura(pubPem, conteudo, hashFalso, assinatura).Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_ChavePublicaErrada_RetornaFalse()
    {
        var (_, privBytes) = GerarParDeChaves();
        var (outraChavePublica, _) = GerarParDeChaves();
        var conteudo = "{\"valor\":200.00}";
        var hashAnterior = new string('0', 64);

        var assinatura = AssinarDer(privBytes, conteudo, hashAnterior);

        _servico.ValidarAssinatura(outraChavePublica, conteudo, hashAnterior, assinatura).Should().BeFalse();
    }

    [Fact]
    public void ValidarAssinatura_AssinaturaBase64Invalida_RetornaFalse()
    {
        var (pubPem, _) = GerarParDeChaves();

        _servico.ValidarAssinatura(pubPem, "{\"valor\":10}", new string('0', 64), "nao-e-base64!!!").Should().BeFalse();
    }

    // ── Cálculo de hash ──────────────────────────────────────────────────────

    [Fact]
    public void CalcularHashTransacao_MesmosParametros_RetornaMesmoHash()
    {
        var conteudo = "{\"valor\":75.00}";
        var hashAnterior = new string('0', 64);
        var assinatura = "assinatura-qualquer";

        var hash1 = _servico.CalcularHashTransacao(conteudo, hashAnterior, assinatura);
        var hash2 = _servico.CalcularHashTransacao(conteudo, hashAnterior, assinatura);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CalcularHashTransacao_RetornaStringHexDe64Caracteres()
    {
        var hash = _servico.CalcularHashTransacao("{\"valor\":1}", new string('0', 64), "sig");

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void CalcularHashTransacao_ParametrosDiferentes_RetornamHashesDiferentes()
    {
        var hashAnterior = new string('0', 64);
        var assinatura = "mesma-assinatura";

        var hash1 = _servico.CalcularHashTransacao("{\"valor\":100}", hashAnterior, assinatura);
        var hash2 = _servico.CalcularHashTransacao("{\"valor\":200}", hashAnterior, assinatura);

        hash1.Should().NotBe(hash2);
    }

    // ── Cadeia de hashes ─────────────────────────────────────────────────────

    [Fact]
    public void CadeiaDeHashes_TresTransacoes_HashEncadeadoCorreto()
    {
        // Simula três transações encadeadas — o hashAnterior de cada uma
        // é o hashAtual da transação anterior.
        var (pubPem, privBytes) = GerarParDeChaves();
        var hashZero = new string('0', 64);

        // Transação 1
        var c1 = "{\"transacaoId\":\"t1\",\"valor\":10}";
        var sig1 = AssinarDer(privBytes, c1, hashZero);
        var hash1 = _servico.CalcularHashTransacao(c1, hashZero, sig1);

        // Transação 2 — hashAnterior = hash1
        var c2 = "{\"transacaoId\":\"t2\",\"valor\":20}";
        var sig2 = AssinarDer(privBytes, c2, hash1);
        var hash2 = _servico.CalcularHashTransacao(c2, hash1, sig2);

        // Transação 3 — hashAnterior = hash2
        var c3 = "{\"transacaoId\":\"t3\",\"valor\":30}";
        var sig3 = AssinarDer(privBytes, c3, hash2);

        _servico.ValidarAssinatura(pubPem, c1, hashZero, sig1).Should().BeTrue();
        _servico.ValidarAssinatura(pubPem, c2, hash1, sig2).Should().BeTrue();
        _servico.ValidarAssinatura(pubPem, c3, hash2, sig3).Should().BeTrue();

        hash1.Should().HaveLength(64);
        hash2.Should().HaveLength(64);
        hash1.Should().NotBe(hash2);
    }
}
