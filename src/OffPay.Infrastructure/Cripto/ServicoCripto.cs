using System.Security.Cryptography;
using System.Text;
using OffPay.Application.Abstractions;

namespace OffPay.Infrastructure.Cripto;

public class ServicoCripto : IServicoCripto
{
    public bool ValidarAssinatura(string chavePublicaPem, string conteudoCanonico, string hashAnterior, string assinaturaBase64)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(chavePublicaPem);

            var dados = Encoding.UTF8.GetBytes(conteudoCanonico + hashAnterior);
            var assinatura = Convert.FromBase64String(assinaturaBase64);

            // DER (ASN.1) — formato nativo do Android Keystore e iOS Secure Enclave
            return ecdsa.VerifyData(dados, assinatura, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch
        {
            return false;
        }
    }

    public string CalcularHashTransacao(string conteudoCanonico, string hashAnterior, string assinaturaBase64)
    {
        var conteudo = Encoding.UTF8.GetBytes(conteudoCanonico + hashAnterior + assinaturaBase64);
        var hash = SHA256.HashData(conteudo);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
