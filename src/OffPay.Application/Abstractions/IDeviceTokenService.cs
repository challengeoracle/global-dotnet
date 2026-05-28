namespace OffPay.Application.Abstractions;

public interface IDeviceTokenService
{
    string GerarDeviceToken(string identificadorPublico);
    bool TentarValidar(string token, out string identificadorPublico);
}
