using Microsoft.EntityFrameworkCore;
using OffPay.Application.Abstractions;
using OffPay.Domain.Entidades;
using OffPay.Domain.Enums;

namespace OffPay.Infrastructure.Persistencia.Oracle.Repositorios;

public class DispositivoRepository : IDispositivoRepository
{
    private readonly OffPayDbContext _context;

    public DispositivoRepository(OffPayDbContext context)
    {
        _context = context;
    }

    public async Task<Dispositivo?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await _context.Dispositivos.FindAsync([id], ct);

    public async Task<Dispositivo?> ObterPorIdentificadorPublicoAsync(string identificadorPublico, CancellationToken ct = default)
        => await _context.Dispositivos
            .FirstOrDefaultAsync(d => d.IdentificadorPublico == identificadorPublico, ct);

    public async Task<(IReadOnlyList<Dispositivo> Itens, int Total)> ListarAsync(
        string? comercianteId,
        StatusDispositivo? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        var query = _context.Dispositivos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(comercianteId))
            query = query.Where(d => d.ComercianteId == comercianteId);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderBy(d => d.DataRegistro)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task AdicionarAsync(Dispositivo dispositivo, CancellationToken ct = default)
        => await _context.Dispositivos.AddAsync(dispositivo, ct);

    public async Task SalvarAlteracoesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
