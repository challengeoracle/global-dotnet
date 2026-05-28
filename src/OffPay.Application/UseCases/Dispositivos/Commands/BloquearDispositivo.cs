using OffPay.Application.Abstractions;
using OffPay.Domain.Exceptions;

namespace OffPay.Application.UseCases.Dispositivos.Commands;

public record BloquearDispositivoCommand(string IdentificadorPublico, string Motivo);

public class BloquearDispositivoHandler
{
    private readonly IDispositivoRepository _repository;

    public BloquearDispositivoHandler(IDispositivoRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(BloquearDispositivoCommand command, CancellationToken ct = default)
    {
        var dispositivo = await _repository.ObterPorIdentificadorPublicoAsync(command.IdentificadorPublico, ct)
            ?? throw new DispositivoNaoEncontradoException(command.IdentificadorPublico);

        dispositivo.Bloquear(command.Motivo);
        await _repository.SalvarAlteracoesAsync(ct);
    }
}
