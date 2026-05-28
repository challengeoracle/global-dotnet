using OffPay.Domain.Entidades;
using OffPay.Domain.Enums;

namespace OffPay.Application.Abstractions;

public interface ILogAuditoriaRepository
{
    Task<LogAuditoria?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<(IReadOnlyList<LogAuditoria> Itens, int Total)> ListarAsync(
        long? dispositivoId,
        DateTime? dataInicio,
        DateTime? dataFim,
        StatusValidacao? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default);
    Task AdicionarAsync(LogAuditoria log, CancellationToken ct = default);
    Task AdicionarVariosAsync(IEnumerable<LogAuditoria> logs, CancellationToken ct = default);
    Task SalvarAlteracoesAsync(CancellationToken ct = default);
}
