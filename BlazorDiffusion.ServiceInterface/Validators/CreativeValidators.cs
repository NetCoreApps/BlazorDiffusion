using BlazorDiffusion.ServiceModel;
using ServiceStack.FluentValidation;

namespace BlazorDiffusion.ServiceInterface.Validators;

public class CreateCreativeValidator : AbstractValidator<CreateCreative>
{
    public CreateCreativeValidator()
    {
        RuleFor(x => x.UserPrompt).NotEmpty();
        RuleFor(x => x.Height)
            .Must(x => x is >= 256 and <= 1024)
            .When(x => x.Height != null)
            .WithMessage("Height must be between 256 and 1024.");
        RuleFor(x => x.Width)
            .Must(x => x is >= 256 and <= 1024)
            .When(x => x.Width != null)
            .WithMessage("Width must be between 256 and 1024.");
        RuleFor(x => x.Images)
            .Must(x => x is > 0 and < 10)
            .When(x => x.Images != null)
            .WithMessage("Images must be between 1 and 9.");
        RuleFor(x => x.Steps)
            .Must(x => x is >= 10 and <= 150)
            .When(x => x.Steps != null)
            .WithMessage("Steps must be between 10 and 150.");
        RuleFor(x => x.ModifierIds).NotEmpty()
            .WithMessage("Must specify at least one Modifier");
    }
}