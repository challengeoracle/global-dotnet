using FluentValidation;
using OffPay.Application.DTOs;

namespace OffPay.Application.Validators;

public class RegistrarDispositivoRequestValidator : AbstractValidator<RegistrarDispositivoRequest>
{
    public RegistrarDispositivoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome e obrigatorio.")
            .MaximumLength(120).WithMessage("Nome deve ter no maximo 120 caracteres.");

        RuleFor(x => x.ComercianteId)
            .NotEmpty().WithMessage("ComercianteId e obrigatorio.")
            .MaximumLength(64).WithMessage("ComercianteId deve ter no maximo 64 caracteres.");

        RuleFor(x => x.ChavePublicaPem)
            .NotEmpty().WithMessage("ChavePublicaPem e obrigatoria.")
            .Must(pem => pem.Contains("BEGIN PUBLIC KEY") || pem.Contains("BEGIN EC PUBLIC KEY"))
            .WithMessage("ChavePublicaPem deve ser uma chave publica PEM valida.");
    }
}
