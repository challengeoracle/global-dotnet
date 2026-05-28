namespace OffPay.Application.Abstractions;

public interface IServicoCripto
{
    /// <summary>
    /// Valida assinatura ECDSA P-256 (SHA-256).
    /// Conteúdo assinado: conteudoCanonico + hashAnterior (concatenação direta UTF-8).
    /// Formato da assinatura esperado: DER (ASN.1), Base64.
    /// </summary>
    bool ValidarAssinatura(string chavePublicaPem, string conteudoCanonico, string hashAnterior, string assinaturaBase64);

    /// <summary>
    /// Calcula o hashAtual da transação: SHA-256 hex de (conteudoCanonico + hashAnterior + assinaturaBase64).
    /// </summary>
    string CalcularHashTransacao(string conteudoCanonico, string hashAnterior, string assinaturaBase64);
}
