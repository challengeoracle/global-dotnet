namespace OffPay.Domain.Exceptions;

public class LogAuditoriaNaoEncontradoException : DomainException
{
    public LogAuditoriaNaoEncontradoException(long id)
        : base($"Log de auditoria com id '{id}' nao encontrado.") { }
}
