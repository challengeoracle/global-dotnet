namespace OffPay.Domain.ValueObjects;

public record AssinaturaDigital(string Base64)
{
    // Assinatura ECDSA P-256 serializada em Base64
    public byte[] ToBytes() => Convert.FromBase64String(Base64);
}
