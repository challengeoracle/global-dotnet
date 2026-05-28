using FluentValidation;
using OffPay.Application.DTOs;

namespace OffPay.Application.Validators;

public class LoteAuditoriaRequestValidator : AbstractValidator<LoteAuditoriaRequest>
{
    public LoteAuditoriaRequestValidator()
    {
        RuleFor(x => x.LoteId)
            .NotEmpty().WithMessage("LoteId e obrigatorio.")
            .MaximumLength(64);

        RuleFor(x => x.DispositivoIdentificador)
            .NotEmpty().WithMessage("DispositivoIdentificador e obrigatorio.")
            .MaximumLength(64);

        RuleFor(x => x.Transacoes)
            .NotEmpty().WithMessage("O lote deve conter pelo menos uma transacao.")
            .Must(t => t.Count <= 1000).WithMessage("O lote nao pode exceder 1000 transacoes.");

        RuleForEach(x => x.Transacoes).ChildRules(t =>
        {
            t.RuleFor(x => x.TransacaoId).NotEmpty().MaximumLength(64);
            t.RuleFor(x => x.ConteudoCanonico).NotEmpty();
            t.RuleFor(x => x.HashAnterior)
                .NotEmpty()
                .Length(64).WithMessage("HashAnterior deve ter exatamente 64 caracteres hexadecimais.")
                .Matches("^[0-9a-f]{64}$").WithMessage("HashAnterior deve ser hexadecimal minusculo.");
            t.RuleFor(x => x.HashAtual)
                .NotEmpty()
                .Length(64).WithMessage("HashAtual deve ter exatamente 64 caracteres hexadecimais.")
                .Matches("^[0-9a-f]{64}$").WithMessage("HashAtual deve ser hexadecimal minusculo.");
            t.RuleFor(x => x.Assinatura).NotEmpty();
        });
    }
}
