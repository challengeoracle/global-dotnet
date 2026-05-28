using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OffPay.Application.Abstractions;

namespace OffPay.Infrastructure.Auth;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly string _key;
    private readonly string _issuer;

    public DeviceTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]!;
        _issuer = configuration["Jwt:Issuer"]!;
    }

    public string GerarDeviceToken(string identificadorPublico)
    {
        var claims = new[]
        {
            new Claim("deviceId", identificadorPublico),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        // Device Token sem expiração fixa — revogado marcando dispositivo como REVOGADO no banco
        var token = new JwtSecurityToken(
            issuer: _issuer,
            claims: claims,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TentarValidar(string token, out string identificadorPublico)
    {
        identificadorPublico = string.Empty;

        var handler = new JwtSecurityTokenHandler();
        var parametros = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = false,
            ValidateLifetime = false
        };

        try
        {
            var principal = handler.ValidateToken(token, parametros, out _);
            identificadorPublico = principal.FindFirstValue("deviceId") ?? string.Empty;
            return !string.IsNullOrEmpty(identificadorPublico);
        }
        catch
        {
            return false;
        }
    }
}
