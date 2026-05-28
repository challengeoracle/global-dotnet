using OffPay.Domain.Enums;
using OffPay.Domain.Exceptions;

namespace OffPay.Domain.Entidades;

public class Dispositivo
{
    public long Id { get; private set; }
    public string IdentificadorPublico { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public string ComercianteId { get; private set; } = default!;
    public string ChavePublicaPem { get; private set; } = default!;
    public StatusDispositivo Status { get; private set; }
    public DateTime DataRegistro { get; private set; }
    public DateTime? DataBloqueio { get; private set; }
    public string? MotivoBloqueio { get; private set; }

    public IReadOnlyCollection<LogAuditoria> Logs => _logs.AsReadOnly();
    private readonly List<LogAuditoria> _logs = [];

    // Construtor privado para EF Core
    private Dispositivo() { }

    public static Dispositivo Criar(string nome, string comercianteId, string chavePublicaPem)
    {
        return new Dispositivo
        {
            IdentificadorPublico = Guid.NewGuid().ToString("N"),
            Nome = nome,
            ComercianteId = comercianteId,
            ChavePublicaPem = chavePublicaPem,
            Status = StatusDispositivo.Ativo,
            DataRegistro = DateTime.UtcNow
        };
    }

    public void Bloquear(string motivo)
    {
        if (Status == StatusDispositivo.Revogado)
            throw new DomainException("Dispositivo revogado nao pode ser bloqueado.");

        Status = StatusDispositivo.Bloqueado;
        DataBloqueio = DateTime.UtcNow;
        MotivoBloqueio = motivo;
    }

    public void RevogarChaves()
    {
        Status = StatusDispositivo.Revogado;
    }

    public bool EstaAtivo() => Status == StatusDispositivo.Ativo;
}
