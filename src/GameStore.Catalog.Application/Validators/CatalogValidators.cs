using FluentValidation;
using GameStore.Catalog.Application.DTOs;

namespace GameStore.Catalog.Application.Validators;

public class CreateGameValidator : AbstractValidator<CreateGameRequest>
{
    public CreateGameValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título do jogo é obrigatório.")
            .Length(2, 150).WithMessage("O título deve ter entre 2 e 150 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(2000).WithMessage("A descrição não pode ter mais de 2000 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("O preço não pode ser negativo.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("A categoria é obrigatória.")
            .MaximumLength(50).WithMessage("A categoria não pode ter mais de 50 caracteres.");
    }
}

public class UpdateGameValidator : AbstractValidator<UpdateGameRequest>
{
    public UpdateGameValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título do jogo é obrigatório.")
            .Length(2, 150).WithMessage("O título deve ter entre 2 e 150 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(2000).WithMessage("A descrição não pode ter mais de 2000 caracteres.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("O preço não pode ser negativo.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("A categoria é obrigatória.")
            .MaximumLength(50).WithMessage("A categoria não pode ter mais de 50 caracteres.");
    }
}

public class CreatePromotionValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título da promoção é obrigatório.")
            .MaximumLength(100).WithMessage("O título não pode exceder 100 caracteres.");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(1, 100).WithMessage("O percentual de desconto deve estar entre 1% e 100%.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("A data de término deve ser posterior à data de início.");
    }
}

