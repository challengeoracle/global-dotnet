using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OffPay.Infrastructure.Persistence.Mongo.Documents;

public class PayloadBrutoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("loteId")]
    public string LoteId { get; set; } = default!;

    [BsonElement("dispositivoIdentificador")]
    public string DispositivoIdentificador { get; set; } = default!;

    [BsonElement("recebidoEm")]
    public DateTime RecebidoEm { get; set; }

    [BsonElement("statusGeral")]
    public string StatusGeral { get; set; } = default!;

    [BsonElement("payloadOriginal")]
    public BsonDocument PayloadOriginal { get; set; } = default!;

    [BsonElement("resultadoValidacao")]
    public ResultadoValidacaoDocument ResultadoValidacao { get; set; } = new();
}

public class ResultadoValidacaoDocument
{
    [BsonElement("transacoesValidadas")]
    public int TransacoesValidadas { get; set; }

    [BsonElement("transacoesRejeitadas")]
    public int TransacoesRejeitadas { get; set; }

    [BsonElement("motivos")]
    public List<string> Motivos { get; set; } = [];
}
