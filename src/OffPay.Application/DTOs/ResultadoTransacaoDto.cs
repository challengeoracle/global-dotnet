namespace OffPay.Application.DTOs;

public record ResultadoTransacaoDto(string TransacaoId, string Status, string? Observacao);
