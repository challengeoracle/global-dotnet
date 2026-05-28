namespace OffPay.Application.DTOs;

public record ListagemDto<T>(
    IReadOnlyList<T> Itens,
    int TotalRegistros,
    int Pagina,
    int Tamanho);
