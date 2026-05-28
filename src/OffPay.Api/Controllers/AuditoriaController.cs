using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;
using OffPay.Application.UseCases.Auditoria.Commands;
using OffPay.Application.UseCases.Auditoria.Queries;
using OffPay.Domain.Enums;

namespace OffPay.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly ReceberLoteHandler _receberLote;
    private readonly ObterLogAuditoriaHandler _obterLog;
    private readonly ListarLogsAuditoriaHandler _listarLogs;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly IValidator<LoteAuditoriaRequest> _validador;

    public AuditoriaController(
        ReceberLoteHandler receberLote,
        ObterLogAuditoriaHandler obterLog,
        ListarLogsAuditoriaHandler listarLogs,
        IDeviceTokenService deviceTokenService,
        IValidator<LoteAuditoriaRequest> validador)
    {
        _receberLote = receberLote;
        _obterLog = obterLog;
        _listarLogs = listarLogs;
        _deviceTokenService = deviceTokenService;
        _validador = validador;
    }

    /// <summary>Recebe um lote de transacoes offline para validacao criptografica e auditoria.</summary>
    /// <remarks>Requer JWT Bearer (usuario) e X-Device-Token (terminal) simultaneamente.</remarks>
    [HttpPost("lote")]
    [ProducesResponseType(typeof(ResultadoLoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReceberLote([FromBody] LoteAuditoriaRequest request, CancellationToken ct)
    {
        var deviceToken = Request.Headers["X-Device-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(deviceToken) || !_deviceTokenService.TentarValidar(deviceToken, out var identificadorPublico))
            return Forbid();

        // O Device Token deve corresponder ao identificador informado no lote
        if (!identificadorPublico.Equals(request.DispositivoIdentificador, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var validacao = await _validador.ValidateAsync(request, ct);
        if (!validacao.IsValid)
        {
            foreach (var erro in validacao.Errors)
                ModelState.AddModelError(erro.PropertyName, erro.ErrorMessage);
            return ValidationProblem();
        }

        var command = new ReceberLoteCommand(
            request.LoteId, request.DispositivoIdentificador, request.GeradoEm, request.Transacoes);

        var resultado = await _receberLote.HandleAsync(command, ct);
        return Ok(resultado);
    }

    /// <summary>Lista logs de auditoria com filtros opcionais e paginacao.</summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ListagemDto<LogAuditoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarLogs(
        [FromQuery] long? dispositivoId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] StatusValidacao? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarLogsAuditoriaQuery(dispositivoId, dataInicio, dataFim, status, pagina, tamanho);
        var resultado = await _listarLogs.HandleAsync(query, ct);
        return Ok(resultado);
    }

    /// <summary>Retorna um log de auditoria pelo seu id.</summary>
    [HttpGet("logs/{id:long}")]
    [ProducesResponseType(typeof(LogAuditoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterLog(long id, CancellationToken ct)
    {
        var query = new ObterLogAuditoriaQuery(id);
        var dto = await _obterLog.HandleAsync(query, ct);
        return Ok(dto);
    }
}
