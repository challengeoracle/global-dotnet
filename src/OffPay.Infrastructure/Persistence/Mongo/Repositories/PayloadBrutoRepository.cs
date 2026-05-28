using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using OffPay.Application.Abstractions;
using OffPay.Infrastructure.Persistence.Mongo.Documents;

namespace OffPay.Infrastructure.Persistence.Mongo.Repositories;

public class PayloadBrutoRepository : IPayloadBrutoRepository
{
    private readonly IMongoCollection<PayloadBrutoDocument> _collection;

    public PayloadBrutoRepository(MongoContext context)
    {
        _collection = context.GetCollection<PayloadBrutoDocument>("payloads_brutos");
    }

    public async Task<string> SalvarPayloadAsync(
        string loteId,
        string dispositivoIdentificador,
        object payloadOriginal,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payloadOriginal);
        var document = new PayloadBrutoDocument
        {
            LoteId = loteId,
            DispositivoIdentificador = dispositivoIdentificador,
            RecebidoEm = DateTime.UtcNow,
            StatusGeral = "PENDENTE",
            PayloadOriginal = BsonDocument.Parse(json),
            ResultadoValidacao = new ResultadoValidacaoDocument()
        };

        await _collection.InsertOneAsync(document, cancellationToken: ct);
        return document.Id!;
    }

    public async Task AtualizarResultadoAsync(
        string id,
        string statusGeral,
        int transacoesValidadas,
        int transacoesRejeitadas,
        List<string> motivos,
        CancellationToken ct = default)
    {
        var filtro = Builders<PayloadBrutoDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<PayloadBrutoDocument>.Update
            .Set(d => d.StatusGeral, statusGeral)
            .Set(d => d.ResultadoValidacao, new ResultadoValidacaoDocument
            {
                TransacoesValidadas = transacoesValidadas,
                TransacoesRejeitadas = transacoesRejeitadas,
                Motivos = motivos
            });

        await _collection.UpdateOneAsync(filtro, update, cancellationToken: ct);
    }
}
