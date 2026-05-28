namespace OffPay.Application.DTOs;

public record TransacaoAuditoriaDto(
    string TransacaoId,
    DateTime Timestamp,
    string ConteudoCanonico,
    string HashAnterior,
    string HashAtual,
    string Assinatura);
