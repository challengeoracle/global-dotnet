namespace OffPay.Application.DTOs;

public record RegistrarDispositivoRequest(string Nome, string ComercianteId, string ChavePublicaPem);
