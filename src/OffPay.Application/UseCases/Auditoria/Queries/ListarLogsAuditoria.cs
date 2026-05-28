using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Enums;

namespace OffPay.Application.UseCases.Auditoria.Queries;

public record ListarLogsAuditoriaQuery(
    long? DispositivoId,
    DateTime? DataInicio,
    DateTime? DataFim,
    StatusValidacao? Status,
    int Pagina,
    int Tamanho);

public class ListarLogsAuditoriaHandler
{
    private readonly ILogAuditoriaRepository _repository;

    public ListarLogsAuditoriaHandler(ILogAuditoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListagemDto<LogAuditoriaDto>> HandleAsync(ListarLogsAuditoriaQuery query, CancellationToken ct = default)
    {
        var (itens, total) = await _repository.ListarAsync(
            query.DispositivoId, query.DataInicio, query.DataFim, query.Status, query.Pagina, query.Tamanho, ct);

        var dtos = itens.Select(LogAuditoriaDto.FromEntity).ToList();
        return new ListagemDto<LogAuditoriaDto>(dtos, total, query.Pagina, query.Tamanho);
    }
}
