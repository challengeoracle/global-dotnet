namespace OffPay.Application.DTOs;

public record LoteAuditoriaRequest(
    string LoteId,
    string DispositivoIdentificador,
    DateTime GeradoEm,
    IReadOnlyList<TransacaoAuditoriaDto> Transacoes);
