namespace OffPay.Domain.Exceptions;

public class DispositivoNaoEncontradoException : DomainException
{
    public DispositivoNaoEncontradoException(string identificador)
        : base($"Dispositivo '{identificador}' nao encontrado.") { }
}
