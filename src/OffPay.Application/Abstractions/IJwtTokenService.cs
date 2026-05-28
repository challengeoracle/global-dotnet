namespace OffPay.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiraEm) GerarToken(string sub, string role);
}
