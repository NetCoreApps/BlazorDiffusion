using BlazorDiffusion.ServiceModel;
using ServiceStack.FluentValidation;

namespace BlazorDiffusion.ServiceInterface.Validators;

public class FindSimilarArtifactsValidator : AbstractValidator<FindSimilarArtifacts>
{
    public FindSimilarArtifactsValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThan(0)
            .When(x => x.Skip != null);
        RuleFor(x => x.Take)
            .GreaterThan(10)
            .LessThan(100)
            .When(x => x.Take != null);
    }
}