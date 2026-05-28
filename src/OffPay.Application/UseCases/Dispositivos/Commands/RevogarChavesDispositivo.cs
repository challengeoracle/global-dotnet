using OffPay.Application.Abstractions;
using OffPay.Domain.Exceptions;

namespace OffPay.Application.UseCases.Dispositivos.Commands;

public record RevogarChavesDispositivoCommand(string IdentificadorPublico);

public class RevogarChavesDispositivoHandler
{
    private readonly IDispositivoRepository _repository;

    public RevogarChavesDispositivoHandler(IDispositivoRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(RevogarChavesDispositivoCommand command, CancellationToken ct = default)
    {
        var dispositivo = await _repository.ObterPorIdentificadorPublicoAsync(command.IdentificadorPublico, ct)
            ?? throw new DispositivoNaoEncontradoException(command.IdentificadorPublico);

        dispositivo.RevogarChaves();
        await _repository.SalvarAlteracoesAsync(ct);
    }
}
