using OffPay.Domain.Entities;

namespace OffPay.Application.DTOs;

public record LogAuditoriaDto(
    long Id,
    long DispositivoId,
    string LoteId,
    string TransacaoId,
    DateTime TimestampTransacao,
    DateTime TimestampRecebimento,
    string StatusValidacao,
    string HashTransacao,
    string HashAnterior,
    string? Observacao)
{
    public static LogAuditoriaDto FromEntity(LogAuditoria l) => new(
        l.Id,
        l.DispositivoId,
        l.LoteId,
        l.TransacaoId,
        l.TimestampTransacao,
        l.TimestampRecebimento,
        l.StatusValidacao.ToString(),
        l.HashTransacao,
        l.HashAnterior,
        l.Observacao);
}
