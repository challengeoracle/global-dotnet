namespace OffPay.Domain.Exceptions;

public class DispositivoInativoException : DomainException
{
    public DispositivoInativoException(string identificador, string status)
        : base($"Dispositivo '{identificador}' nao esta ativo (status: {status}).") { }
}
