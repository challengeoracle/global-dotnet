using System.Security.Cryptography;
using System.Text;

using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

var chavePublicaPem = ecdsa.ExportSubjectPublicKeyInfoPem();
var chavePublicaJson = chavePublicaPem.Replace("\r", "").Replace("\n", "\\n");

// Transação 1
var conteudo1 = "{\"comercianteId\":\"comerciante-uuid-teste\",\"timestamp\":\"2026-05-28T14:25:00Z\",\"valor\":150.00}";
var conteudo1Json = conteudo1.Replace("\"", "\\\"");
var hash0 = new string('0', 64);
var sig1Bytes = ecdsa.SignData(Encoding.UTF8.GetBytes(conteudo1 + hash0), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
var sig1 = Convert.ToBase64String(sig1Bytes);
var hash1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(conteudo1 + hash0 + sig1))).ToLowerInvariant();

// Transação 2
var conteudo2 = "{\"comercianteId\":\"comerciante-uuid-teste\",\"timestamp\":\"2026-05-28T14:26:00Z\",\"valor\":75.50}";
var conteudo2Json = conteudo2.Replace("\"", "\\\"");
var sig2Bytes = ecdsa.SignData(Encoding.UTF8.GetBytes(conteudo2 + hash1), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
var sig2 = Convert.ToBase64String(sig2Bytes);
var hash2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(conteudo2 + hash1 + sig2))).ToLowerInvariant();

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  PASSO 1 — Registrar dispositivo (POST /api/dispositivos)    ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine($$"""
{
  "nome": "Caixa 01 — Loja Centro",
  "comercianteId": "comerciante-uuid-teste",
  "chavePublicaPem": "{{chavePublicaJson}}"
}
""");

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  PASSO 2 — Enviar lote (POST /api/auditoria/lote)            ║");
Console.WriteLine("║  Substitua DEVICE_TOKEN e IDENTIFICADOR pelo da resposta     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine($$"""
{
  "loteId": "550e8400-e29b-41d4-a716-446655440000",
  "dispositivoIdentificador": "IDENTIFICADOR_PUBLICO_DA_RESPOSTA",
  "geradoEm": "2026-05-28T14:30:00Z",
  "transacoes": [
    {
      "transacaoId": "660e8400-e29b-41d4-a716-446655440001",
      "timestamp": "2026-05-28T14:25:00Z",
      "conteudoCanonico": "{{conteudo1Json}}",
      "hashAnterior": "{{hash0}}",
      "hashAtual": "{{hash1}}",
      "assinatura": "{{sig1}}"
    },
    {
      "transacaoId": "660e8400-e29b-41d4-a716-446655440002",
      "timestamp": "2026-05-28T14:26:00Z",
      "conteudoCanonico": "{{conteudo2Json}}",
      "hashAnterior": "{{hash1}}",
      "hashAtual": "{{hash2}}",
      "assinatura": "{{sig2}}"
    }
  ]
}
""");
