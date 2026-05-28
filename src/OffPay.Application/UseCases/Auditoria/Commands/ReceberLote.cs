using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Domain.Entities;
using OffPay.Domain.Enums;
using OffPay.Domain.Exceptions;

namespace OffPay.Application.UseCases.Auditoria.Commands;

public record ReceberLoteCommand(
    string LoteId,
    string DispositivoIdentificador,
    DateTime GeradoEm,
    IReadOnlyList<TransacaoAuditoriaDto> Transacoes);

public class ReceberLoteHandler
{
    private readonly IDispositivoRepository _dispositivoRepository;
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IPayloadBrutoRepository _payloadBrutoRepository;
    private readonly IServicoCripto _servicoCripto;

    public ReceberLoteHandler(
        IDispositivoRepository dispositivoRepository,
        ILogAuditoriaRepository logAuditoriaRepository,
        IPayloadBrutoRepository payloadBrutoRepository,
        IServicoCripto servicoCripto)
    {
        _dispositivoRepository = dispositivoRepository;
        _logAuditoriaRepository = logAuditoriaRepository;
        _payloadBrutoRepository = payloadBrutoRepository;
        _servicoCripto = servicoCripto;
    }

    public async Task<ResultadoLoteDto> HandleAsync(ReceberLoteCommand command, CancellationToken ct = default)
    {
        var dispositivo = await _dispositivoRepository.ObterPorIdentificadorPublicoAsync(command.DispositivoIdentificador, ct)
            ?? throw new DispositivoNaoEncontradoException(command.DispositivoIdentificador);

        // Persiste payload bruto antes de qualquer validação — evidência forense
        var mongoId = await _payloadBrutoRepository.SalvarPayloadAsync(
            command.LoteId, command.DispositivoIdentificador, command, ct);

        if (!dispositivo.EstaAtivo())
        {
            var logsRejeitados = command.Transacoes.Select(t => LogAuditoria.Criar(
                dispositivo.Id, command.LoteId, t.TransacaoId, t.Timestamp,
                StatusValidacao.DispositivoBloqueado, t.HashAtual, t.HashAnterior,
                $"Dispositivo com status {dispositivo.Status}.")).ToList();

            await _logAuditoriaRepository.AdicionarVariosAsync(logsRejeitados, ct);
            await _logAuditoriaRepository.SalvarAlteracoesAsync(ct);
            await _payloadBrutoRepository.AtualizarResultadoAsync(
                mongoId, "REJEITADO_TOTAL", 0, command.Transacoes.Count,
                [$"Dispositivo com status {dispositivo.Status}."], ct);

            throw new DispositivoInativoException(dispositivo.IdentificadorPublico, dispositivo.Status.ToString());
        }

        var transacoesOrdenadas = command.Transacoes.OrderBy(t => t.Timestamp).ToList();
        var resultados = new List<ResultadoTransacaoDto>();
        var hashAnteriorEsperado = new string('0', 64);
        var cadeiaQuebrada = false;

        foreach (var transacao in transacoesOrdenadas)
        {
            StatusValidacao status;
            string? observacao = null;

            if (cadeiaQuebrada)
            {
                status = StatusValidacao.HashQuebrado;
                observacao = "Cadeia de hashes quebrada em transacao anterior.";
            }
            else if (transacao.HashAnterior != hashAnteriorEsperado)
            {
                status = StatusValidacao.HashQuebrado;
                observacao = "HashAnterior declarado nao confere com hashAtual da transacao anterior.";
                cadeiaQuebrada = true;
            }
            else
            {
                var hashRecalculado = _servicoCripto.CalcularHashTransacao(
                    transacao.ConteudoCanonico, transacao.HashAnterior, transacao.Assinatura);

                if (hashRecalculado != transacao.HashAtual)
                {
                    status = StatusValidacao.HashQuebrado;
                    observacao = "HashAtual declarado diverge do recalculado.";
                    cadeiaQuebrada = true;
                }
                else if (!_servicoCripto.ValidarAssinatura(
                    dispositivo.ChavePublicaPem, transacao.ConteudoCanonico,
                    transacao.HashAnterior, transacao.Assinatura))
                {
                    status = StatusValidacao.AssinaturaInvalida;
                    observacao = "Assinatura ECDSA invalida.";
                    // Assinatura inválida não quebra a cadeia — próximas transações continuam
                    hashAnteriorEsperado = transacao.HashAtual;
                }
                else
                {
                    status = StatusValidacao.Validado;
                    hashAnteriorEsperado = transacao.HashAtual;
                }
            }

            var log = LogAuditoria.Criar(
                dispositivo.Id, command.LoteId, transacao.TransacaoId,
                transacao.Timestamp, status, transacao.HashAtual, transacao.HashAnterior, observacao);

            await _logAuditoriaRepository.AdicionarAsync(log, ct);
            resultados.Add(new ResultadoTransacaoDto(transacao.TransacaoId, status.ToString(), observacao));
        }

        await _logAuditoriaRepository.SalvarAlteracoesAsync(ct);

        var validadas = resultados.Count(r => r.Status == nameof(StatusValidacao.Validado));
        var rejeitadas = resultados.Count - validadas;
        var statusGeral = rejeitadas == 0 ? "VALIDADO" : validadas == 0 ? "REJEITADO_TOTAL" : "REJEITADO_PARCIAL";
        var motivos = resultados
            .Where(r => r.Observacao != null)
            .Select(r => r.Observacao!)
            .Distinct()
            .ToList();

        await _payloadBrutoRepository.AtualizarResultadoAsync(mongoId, statusGeral, validadas, rejeitadas, motivos, ct);

        return new ResultadoLoteDto(command.LoteId, validadas, rejeitadas, resultados);
    }
}
