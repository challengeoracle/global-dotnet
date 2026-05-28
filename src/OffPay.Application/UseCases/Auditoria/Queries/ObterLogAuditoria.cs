using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Exceptions;

namespace OffPay.Application.UseCases.Auditoria.Queries;

public record ObterLogAuditoriaQuery(long Id);

public class ObterLogAuditoriaHandler
{
    private readonly ILogAuditoriaRepository _repository;

    public ObterLogAuditoriaHandler(ILogAuditoriaRepository repository)
    {
        _repository = repository;
    }

    public async Task<LogAuditoriaDto> HandleAsync(ObterLogAuditoriaQuery query, CancellationToken ct = default)
    {
        var log = await _repository.ObterPorIdAsync(query.Id, ct)
            ?? throw new LogAuditoriaNaoEncontradoException(query.Id);

        return LogAuditoriaDto.FromEntity(log);
    }
}
