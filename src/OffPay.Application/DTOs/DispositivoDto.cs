using OffPay.Domain.Entities;

namespace OffPay.Application.DTOs;

public record DispositivoDto(
    long Id,
    string IdentificadorPublico,
    string Nome,
    string ComercianteId,
    string Status,
    DateTime DataRegistro,
    DateTime? DataBloqueio,
    string? MotivoBloqueio)
{
    public static DispositivoDto FromEntity(Dispositivo d) => new(
        d.Id,
        d.IdentificadorPublico,
        d.Nome,
        d.ComercianteId,
        d.Status.ToString(),
        d.DataRegistro,
        d.DataBloqueio,
        d.MotivoBloqueio);
}
