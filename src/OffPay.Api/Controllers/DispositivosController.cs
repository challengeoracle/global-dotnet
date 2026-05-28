using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OffPay.Application.DTOs;
using OffPay.Application.UseCases.Dispositivos.Commands;
using OffPay.Application.UseCases.Dispositivos.Queries;
using OffPay.Domain.Enums;

namespace OffPay.Api.Controllers;

[ApiController]
[Route("api/dispositivos")]
[Authorize]
public class DispositivosController : ControllerBase
{
    private readonly RegistrarDispositivoHandler _registrar;
    private readonly BloquearDispositivoHandler _bloquear;
    private readonly RevogarChavesDispositivoHandler _revogar;
    private readonly ObterDispositivoHandler _obter;
    private readonly ListarDispositivosHandler _listar;
    private readonly IValidator<RegistrarDispositivoRequest> _validadorRegistrar;
    private readonly IValidator<BloquearDispositivoRequest> _validadorBloquear;

    public DispositivosController(
        RegistrarDispositivoHandler registrar,
        BloquearDispositivoHandler bloquear,
        RevogarChavesDispositivoHandler revogar,
        ObterDispositivoHandler obter,
        ListarDispositivosHandler listar,
        IValidator<RegistrarDispositivoRequest> validadorRegistrar,
        IValidator<BloquearDispositivoRequest> validadorBloquear)
    {
        _registrar = registrar;
        _bloquear = bloquear;
        _revogar = revogar;
        _obter = obter;
        _listar = listar;
        _validadorRegistrar = validadorRegistrar;
        _validadorBloquear = validadorBloquear;
    }

    /// <summary>Registra um novo dispositivo e retorna o Device Token.</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(RegistrarDispositivoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDispositivoRequest request, CancellationToken ct)
    {
        var validacao = await _validadorRegistrar.ValidateAsync(request, ct);
        if (!validacao.IsValid)
        {
            foreach (var erro in validacao.Errors)
                ModelState.AddModelError(erro.PropertyName, erro.ErrorMessage);
            return ValidationProblem();
        }

        var command = new RegistrarDispositivoCommand(request.Nome, request.ComercianteId, request.ChavePublicaPem);
        var response = await _registrar.HandleAsync(command, ct);

        return CreatedAtAction(nameof(ObterPorIdentificador), new { identificadorPublico = response.IdentificadorPublico }, response);
    }

    /// <summary>Lista dispositivos com filtros opcionais e paginacao.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListagemDto<DispositivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? comercianteId,
        [FromQuery] StatusDispositivo? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarDispositivosQuery(comercianteId, status, pagina, tamanho);
        var resultado = await _listar.HandleAsync(query, ct);
        return Ok(resultado);
    }

    /// <summary>Retorna um dispositivo pelo seu identificador publico.</summary>
    [HttpGet("{identificadorPublico}")]
    [ProducesResponseType(typeof(DispositivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorIdentificador(string identificadorPublico, CancellationToken ct)
    {
        var query = new ObterDispositivoQuery(identificadorPublico);
        var dto = await _obter.HandleAsync(query, ct);
        return Ok(dto);
    }

    /// <summary>Bloqueia um dispositivo ativo.</summary>
    [HttpPatch("{identificadorPublico}/bloqueio")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Bloquear(string identificadorPublico, [FromBody] BloquearDispositivoRequest request, CancellationToken ct)
    {
        var validacao = await _validadorBloquear.ValidateAsync(request, ct);
        if (!validacao.IsValid)
        {
            foreach (var erro in validacao.Errors)
                ModelState.AddModelError(erro.PropertyName, erro.ErrorMessage);
            return ValidationProblem();
        }

        var command = new BloquearDispositivoCommand(identificadorPublico, request.Motivo);
        await _bloquear.HandleAsync(command, ct);
        return NoContent();
    }

    /// <summary>Revoga as chaves de um dispositivo (marca como REVOGADO).</summary>
    [HttpDelete("{identificadorPublico}/chaves")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevogarChaves(string identificadorPublico, CancellationToken ct)
    {
        var command = new RevogarChavesDispositivoCommand(identificadorPublico);
        await _revogar.HandleAsync(command, ct);
        return NoContent();
    }
}
