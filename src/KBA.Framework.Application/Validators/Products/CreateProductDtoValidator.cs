using FluentValidation;
using KBA.Framework.Application.DTOs.Products;

namespace KBA.Framework.Application.Validators.Products;

/// <summary>
/// Validator pour CreateProductDto
/// </summary>
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du produit est obligatoire.")
            .MaximumLength(200).WithMessage("Le nom du produit ne peut pas dépasser 200 caractères.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La description ne peut pas dépasser 1000 caractères.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Le prix doit être supérieur ou égal à 0.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Le stock doit être supérieur ou égal à 0.");

        RuleFor(x => x.SKU)
            .MaximumLength(50).WithMessage("Le SKU ne peut pas dépasser 50 caractères.")
            .When(x => !string.IsNullOrEmpty(x.SKU));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("La catégorie ne peut pas dépasser 100 caractères.")
            .When(x => !string.IsNullOrEmpty(x.Category));
    }
}
