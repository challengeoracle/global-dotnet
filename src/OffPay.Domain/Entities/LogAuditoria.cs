using OffPay.Domain.Enums;

namespace OffPay.Domain.Entities;

public class LogAuditoria
{
    public long Id { get; private set; }
    public long DispositivoId { get; private set; }
    public Dispositivo Dispositivo { get; private set; } = default!;
    public string LoteId { get; private set; } = default!;
    public string TransacaoId { get; private set; } = default!;
    public DateTime TimestampTransacao { get; private set; }
    public DateTime TimestampRecebimento { get; private set; }
    public StatusValidacao StatusValidacao { get; private set; }
    public string HashTransacao { get; private set; } = default!;
    public string HashAnterior { get; private set; } = default!;
    public string? Observacao { get; private set; }

    // Construtor privado para EF Core
    private LogAuditoria() { }

    public static LogAuditoria Criar(
        long dispositivoId,
        string loteId,
        string transacaoId,
        DateTime timestampTransacao,
        StatusValidacao statusValidacao,
        string hashTransacao,
        string hashAnterior,
        string? observacao = null)
    {
        return new LogAuditoria
        {
            DispositivoId = dispositivoId,
            LoteId = loteId,
            TransacaoId = transacaoId,
            TimestampTransacao = timestampTransacao,
            TimestampRecebimento = DateTime.UtcNow,
            StatusValidacao = statusValidacao,
            HashTransacao = hashTransacao,
            HashAnterior = hashAnterior,
            Observacao = observacao
        };
    }
}
