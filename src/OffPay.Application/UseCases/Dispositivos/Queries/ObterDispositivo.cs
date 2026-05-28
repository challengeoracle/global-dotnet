using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Exceptions;

namespace OffPay.Application.UseCases.Dispositivos.Queries;

public record ObterDispositivoQuery(string IdentificadorPublico);

public class ObterDispositivoHandler
{
    private readonly IDispositivoRepository _repository;

    public ObterDispositivoHandler(IDispositivoRepository repository)
    {
        _repository = repository;
    }

    public async Task<DispositivoDto> HandleAsync(ObterDispositivoQuery query, CancellationToken ct = default)
    {
        var dispositivo = await _repository.ObterPorIdentificadorPublicoAsync(query.IdentificadorPublico, ct)
            ?? throw new DispositivoNaoEncontradoException(query.IdentificadorPublico);

        return DispositivoDto.FromEntity(dispositivo);
    }
}
