using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Enums;

namespace OffPay.Application.UseCases.Dispositivos.Queries;

public record ListarDispositivosQuery(
    string? ComercianteId,
    StatusDispositivo? Status,
    int Pagina,
    int Tamanho);

public class ListarDispositivosHandler
{
    private readonly IDispositivoRepository _repository;

    public ListarDispositivosHandler(IDispositivoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListagemDto<DispositivoDto>> HandleAsync(ListarDispositivosQuery query, CancellationToken ct = default)
    {
        var (itens, total) = await _repository.ListarAsync(query.ComercianteId, query.Status, query.Pagina, query.Tamanho, ct);

        var dtos = itens.Select(DispositivoDto.FromEntity).ToList();

        return new ListagemDto<DispositivoDto>(dtos, total, query.Pagina, query.Tamanho);
    }
}
