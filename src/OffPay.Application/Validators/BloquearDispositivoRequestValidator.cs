using FluentValidation;
using OffPay.Application.DTOs;

namespace OffPay.Application.Validators;

public class BloquearDispositivoRequestValidator : AbstractValidator<BloquearDispositivoRequest>
{
    public BloquearDispositivoRequestValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Motivo e obrigatorio.")
            .MaximumLength(500).WithMessage("Motivo deve ter no maximo 500 caracteres.");
    }
}
