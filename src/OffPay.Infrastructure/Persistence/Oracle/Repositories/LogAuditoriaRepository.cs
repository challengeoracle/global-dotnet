using Microsoft.EntityFrameworkCore;
using OffPay.Application.Abstractions;
using OffPay.Domain.Entities;
using OffPay.Domain.Enums;

namespace OffPay.Infrastructure.Persistence.Oracle.Repositories;

public class LogAuditoriaRepository : ILogAuditoriaRepository
{
    private readonly OffPayDbContext _context;

    public LogAuditoriaRepository(OffPayDbContext context)
    {
        _context = context;
    }

    public async Task<LogAuditoria?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await _context.LogsAuditoria
            .Include(l => l.Dispositivo)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<(IReadOnlyList<LogAuditoria> Itens, int Total)> ListarAsync(
        long? dispositivoId,
        DateTime? dataInicio,
        DateTime? dataFim,
        StatusValidacao? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        var query = _context.LogsAuditoria.AsQueryable();

        if (dispositivoId.HasValue)
            query = query.Where(l => l.DispositivoId == dispositivoId.Value);

        if (dataInicio.HasValue)
            query = query.Where(l => l.TimestampTransacao >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(l => l.TimestampTransacao <= dataFim.Value);

        if (status.HasValue)
            query = query.Where(l => l.StatusValidacao == status.Value);

        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderByDescending(l => l.TimestampRecebimento)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task AdicionarAsync(LogAuditoria log, CancellationToken ct = default)
        => await _context.LogsAuditoria.AddAsync(log, ct);

    public async Task AdicionarVariosAsync(IEnumerable<LogAuditoria> logs, CancellationToken ct = default)
        => await _context.LogsAuditoria.AddRangeAsync(logs, ct);

    public async Task SalvarAlteracoesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
