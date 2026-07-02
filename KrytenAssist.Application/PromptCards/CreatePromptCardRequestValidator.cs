using FluentValidation;

namespace KrytenAssist.Application.PromptCards;

public sealed class CreatePromptCardRequestValidator : AbstractValidator<CreatePromptCardRequest>
{
    public CreatePromptCardRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.PromptText)
            .NotEmpty()
            .MaximumLength(4000);

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(50);
    }
}