namespace OffPay.Application.Abstractions;

public interface IPayloadBrutoRepository
{
    Task<string> SalvarPayloadAsync(
        string loteId,
        string dispositivoIdentificador,
        object payloadOriginal,
        CancellationToken ct = default);

    Task AtualizarResultadoAsync(
        string id,
        string statusGeral,
        int transacoesValidadas,
        int transacoesRejeitadas,
        List<string> motivos,
        CancellationToken ct = default);
}
