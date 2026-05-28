using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

const string BASE_URL = "http://localhost:5267";
const string USUARIO = "admin";
const string SENHA = "Admin@2026";

var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var http = new HttpClient { BaseAddress = new Uri(BASE_URL) };

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║           OffPay — Demo completo de auditoria                ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// ── 1. Login ─────────────────────────────────────────────────────────────────
Console.Write("[ 1/5 ] Login como admin... ");
var loginResp = await http.PostAsJsonAsync("/api/auth/login", new { usuario = USUARIO, senha = SENHA });
if (!loginResp.IsSuccessStatusCode)
{
    Falhou($"HTTP {(int)loginResp.StatusCode} — verifique se a API está rodando em {BASE_URL}");
    return;
}
var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponse>(json);
var jwt = loginBody!.Token;
http.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
OK($"JWT obtido (expira em {loginBody.ExpiraEm:HH:mm})");

// ── 2. Gerar par de chaves ECDSA P-256 ───────────────────────────────────────
Console.Write("[ 2/5 ] Gerando par de chaves ECDSA P-256... ");
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var chavePublicaJson = ecdsa.ExportSubjectPublicKeyInfoPem().Replace("\r", "").Replace("\n", "\\n");
OK("par gerado em memória (chave privada nunca sai deste processo)");

// ── 3. Registrar dispositivo ─────────────────────────────────────────────────
Console.Write("[ 3/5 ] Registrando dispositivo... ");
var regResp = await http.PostAsJsonAsync("/api/dispositivos", new
{
    nome = "Caixa Demo — FIAP GS2026",
    comercianteId = "comerciante-demo-fiap",
    chavePublicaPem = chavePublicaJson.Replace("\\n", "\n")
});
if (!regResp.IsSuccessStatusCode)
{
    var err = await regResp.Content.ReadAsStringAsync();
    Falhou($"HTTP {(int)regResp.StatusCode}: {err}");
    return;
}
var regBody = await regResp.Content.ReadFromJsonAsync<RegistroResponse>(json);
var identificadorPublico = regBody!.IdentificadorPublico;
var deviceToken = regBody.DeviceToken;
OK($"id público: {identificadorPublico}");

// ── 4. Montar e enviar lote com 2 transações assinadas ───────────────────────
Console.Write("[ 4/5 ] Assinando e enviando lote (2 transações)... ");

var c1 = "{\"comercianteId\":\"comerciante-demo-fiap\",\"timestamp\":\"2026-05-28T14:25:00Z\",\"valor\":150.00}";
var c2 = "{\"comercianteId\":\"comerciante-demo-fiap\",\"timestamp\":\"2026-05-28T14:26:00Z\",\"valor\":75.50}";
var hash0 = new string('0', 64);

var (sig1, hash1) = Assinar(ecdsa, c1, hash0);
var (sig2, hash2) = Assinar(ecdsa, c2, hash1);

var loteReq = new HttpRequestMessage(HttpMethod.Post, "/api/auditoria/lote");
loteReq.Headers.Add("X-Device-Token", deviceToken);
loteReq.Content = JsonContent.Create(new
{
    loteId = Guid.NewGuid().ToString(),
    dispositivoIdentificador = identificadorPublico,
    geradoEm = DateTime.UtcNow,
    transacoes = new[]
    {
        new { transacaoId = Guid.NewGuid().ToString(), timestamp = new DateTime(2026, 5, 28, 14, 25, 0, DateTimeKind.Utc),
              conteudoCanonico = c1, hashAnterior = hash0, hashAtual = hash1, assinatura = sig1 },
        new { transacaoId = Guid.NewGuid().ToString(), timestamp = new DateTime(2026, 5, 28, 14, 26, 0, DateTimeKind.Utc),
              conteudoCanonico = c2, hashAnterior = hash1, hashAtual = hash2, assinatura = sig2 }
    }
}, options: json);

var loteResp = await http.SendAsync(loteReq);
if (!loteResp.IsSuccessStatusCode)
{
    var err = await loteResp.Content.ReadAsStringAsync();
    Falhou($"HTTP {(int)loteResp.StatusCode}: {err}");
    return;
}
var loteBody = await loteResp.Content.ReadFromJsonAsync<ResultadoLote>(json);
OK($"{loteBody!.TransacoesValidadas} validadas, {loteBody.TransacoesRejeitadas} rejeitadas");
foreach (var r in loteBody.Resultados)
    Console.WriteLine($"         → {r.TransacaoId[..8]}...  status: {r.Status}");

// ── 5. Consultar logs no Oracle ──────────────────────────────────────────────
Console.Write("[ 5/5 ] Consultando logs de auditoria no Oracle... ");
var logsResp = await http.GetFromJsonAsync<LogsResponse>($"/api/auditoria/logs?pagina=1&tamanho=5", json);
OK($"{logsResp!.TotalRegistros} log(s) no banco");

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Demo concluído com sucesso!                                 ║");
Console.WriteLine("║  Fluxo completo: login → dispositivo → lote → auditoria     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

// ── Helpers ───────────────────────────────────────────────────────────────────
static (string assinatura, string hashAtual) Assinar(ECDsa ecdsa, string conteudo, string hashAnterior)
{
    var dados = Encoding.UTF8.GetBytes(conteudo + hashAnterior);
    var sigBytes = ecdsa.SignData(dados, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    var sig = Convert.ToBase64String(sigBytes);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(conteudo + hashAnterior + sig))).ToLowerInvariant();
    return (sig, hash);
}

static void OK(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write(" OK");
    Console.ResetColor();
    Console.WriteLine($" — {msg}");
}

static void Falhou(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write(" FALHOU");
    Console.ResetColor();
    Console.WriteLine($" — {msg}");
}

// ── Records de resposta ───────────────────────────────────────────────────────
record LoginResponse(string Token, DateTime ExpiraEm);
record RegistroResponse(long Id, string IdentificadorPublico, string DeviceToken);
record ResultadoTransacao(string TransacaoId, string Status, string? Observacao);
record ResultadoLote(string LoteId, int TransacoesValidadas, int TransacoesRejeitadas, List<ResultadoTransacao> Resultados);
record LogsResponse(int TotalRegistros);
