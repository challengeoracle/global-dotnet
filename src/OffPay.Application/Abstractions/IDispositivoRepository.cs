using OffPay.Domain.Entities;
using OffPay.Domain.Enums;

namespace OffPay.Application.Abstractions;

public interface IDispositivoRepository
{
    Task<Dispositivo?> ObterPorIdAsync(long id, CancellationToken ct = default);
    Task<Dispositivo?> ObterPorIdentificadorPublicoAsync(string identificadorPublico, CancellationToken ct = default);
    Task<(IReadOnlyList<Dispositivo> Itens, int Total)> ListarAsync(
        string? comercianteId,
        StatusDispositivo? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default);
    Task AdicionarAsync(Dispositivo dispositivo, CancellationToken ct = default);
    Task SalvarAlteracoesAsync(CancellationToken ct = default);
}
