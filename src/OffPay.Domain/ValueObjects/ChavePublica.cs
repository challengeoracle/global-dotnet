namespace OffPay.Domain.ValueObjects;

public record ChavePublica(string Pem)
{
    // Chave pública ECDSA P-256 em formato PEM (SubjectPublicKeyInfo)
    public bool EhValida() => !string.IsNullOrWhiteSpace(Pem)
        && Pem.Contains("BEGIN PUBLIC KEY");
}
