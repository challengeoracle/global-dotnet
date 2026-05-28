using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Entities;

namespace OffPay.Application.UseCases.Dispositivos.Commands;

public record RegistrarDispositivoCommand(string Nome, string ComercianteId, string ChavePublicaPem);

public class RegistrarDispositivoHandler
{
    private readonly IDispositivoRepository _repository;
    private readonly IDeviceTokenService _deviceTokenService;

    public RegistrarDispositivoHandler(IDispositivoRepository repository, IDeviceTokenService deviceTokenService)
    {
        _repository = repository;
        _deviceTokenService = deviceTokenService;
    }

    public async Task<RegistrarDispositivoResponse> HandleAsync(RegistrarDispositivoCommand command, CancellationToken ct = default)
    {
        var dispositivo = Dispositivo.Criar(command.Nome, command.ComercianteId, command.ChavePublicaPem);

        await _repository.AdicionarAsync(dispositivo, ct);
        await _repository.SalvarAlteracoesAsync(ct);

        var deviceToken = _deviceTokenService.GerarDeviceToken(dispositivo.IdentificadorPublico);

        return new RegistrarDispositivoResponse(dispositivo.Id, dispositivo.IdentificadorPublico, deviceToken);
    }
}
