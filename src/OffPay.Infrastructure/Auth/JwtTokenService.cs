using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OffPay.Application.Abstractions;

namespace OffPay.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiracaoHoras;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]!;
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        _expiracaoHoras = int.Parse(configuration["Jwt:ExpiracaoHoras"] ?? "8");
    }

    public (string Token, DateTime ExpiraEm) GerarToken(string sub, string role)
    {
        var expiraEm = DateTime.UtcNow.AddHours(_expiracaoHoras);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, sub),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
