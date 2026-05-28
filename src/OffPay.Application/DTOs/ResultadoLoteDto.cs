namespace OffPay.Application.DTOs;

public record ResultadoLoteDto(
    string LoteId,
    int TransacoesValidadas,
    int TransacoesRejeitadas,
    IReadOnlyList<ResultadoTransacaoDto> Resultados);
