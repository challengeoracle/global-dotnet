# Exemplos de Uso da API — OffPay

Arquivos `.http` para testar todos os endpoints com o [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) do VS Code.

## Pré-requisitos

1. API em execução (`dotnet run --project src/OffPay.Api`)
2. Oracle e MongoDB configurados e acessíveis
3. Extensão **REST Client** instalada no VS Code

## Ordem de Execução

### 1. [`registrar-dispositivo.http`](registrar-dispositivo.http)

| Passo | Descrição |
|---|---|
| Login | `POST /api/auth/login` → salva o JWT |
| Registrar | `POST /api/dispositivos` → salva identificadorPublico + deviceToken |
| Listar | `GET /api/dispositivos` |
| Buscar | `GET /api/dispositivos/{id}` |
| Bloquear | `PATCH /api/dispositivos/{id}/bloqueio` |
| Revogar | `DELETE /api/dispositivos/{id}/chaves` |
| Health | `GET /health`, `/health/ready`, `/health/live` |

### 2. [`enviar-lote-auditoria.http`](enviar-lote-auditoria.http)

| Passo | Descrição |
|---|---|
| Enviar lote | `POST /api/auditoria/lote` — requer JWT **e** X-Device-Token |
| Listar logs | `GET /api/auditoria/logs` |
| Filtrar logs | Por dispositivo, status, período |
| Buscar log | `GET /api/auditoria/logs/{id}` |
| Dispositivo bloqueado | Cenário de rejeição com registro de evidência |

## Como Gerar os Campos Criptográficos

Para testar o envio de lote com assinatura válida, o mobile precisa:

1. **Gerar par de chaves ECDSA P-256** (Android Keystore / iOS Secure Enclave / `react-native-quick-crypto`)
2. **Registrar o dispositivo** com a chave pública PEM
3. **Para cada transação:**
   - Construir `conteudoCanonico`: JSON com chaves ordenadas, sem espaços
   - Calcular `hashAnterior`: `"000...000"` (64 zeros) na 1ª transação; `hashAtual` da anterior nas seguintes
   - Assinar: `ECDSA.Sign(SHA256(conteudoCanonico + hashAnterior))` — formato DER, Base64
   - Calcular `hashAtual`: `SHA256(conteudoCanonico + hashAnterior + assinaturaBase64)` — hex minúsculo

## Snippet .NET para Gerar Chave de Teste

```csharp
using System.Security.Cryptography;

using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

// Chave pública (para registrar o dispositivo)
Console.WriteLine(ecdsa.ExportSubjectPublicKeyInfoPem());

// Assinar uma transação de teste
var conteudoCanonico = "{\"valor\":100.00}";
var hashAnterior = new string('0', 64);
var dados = System.Text.Encoding.UTF8.GetBytes(conteudoCanonico + hashAnterior);
var assinaturaBytes = ecdsa.SignData(dados, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
Console.WriteLine(Convert.ToBase64String(assinaturaBytes));

// Hash da transação
var conteudoHash = System.Text.Encoding.UTF8.GetBytes(
    conteudoCanonico + hashAnterior + Convert.ToBase64String(assinaturaBytes));
Console.WriteLine(Convert.ToHexString(SHA256.HashData(conteudoHash)).ToLowerInvariant());
```
