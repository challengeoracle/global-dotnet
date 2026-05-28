using Microsoft.AspNetCore.Mvc;
using OffPay.Application.Abstractions;
using OffPay.Application.DTOs;

namespace OffPay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(IJwtTokenService jwtTokenService, IConfiguration configuration)
    {
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    /// <summary>Autentica um usuario e retorna um JWT Bearer.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var usuarioAdmin = _configuration["Auth:AdminUsuario"];
        var senhaAdmin = _configuration["Auth:AdminSenha"];

        if (request.Usuario == usuarioAdmin && request.Senha == senhaAdmin)
        {
            var (token, expiraEm) = _jwtTokenService.GerarToken(request.Usuario, "admin");
            return Ok(new LoginResponse(token, expiraEm.ToString("O")));
        }

        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Credenciais invalidas",
            Detail = "Usuario ou senha incorretos."
        });
    }
}
