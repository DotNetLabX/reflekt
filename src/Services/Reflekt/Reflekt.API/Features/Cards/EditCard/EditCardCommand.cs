using FluentValidation;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.Cards.EditCard;

public sealed record EditCardCommand(int SprintId, int CardId, string Text, string? AssigneeName);

public sealed class EditCardCommandValidator : Validator<EditCardCommand>
{
    public EditCardCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Card.TextMaxLength);
        RuleFor(x => x.AssigneeName).MaximumLength(Card.AssigneeNameMaxLength).When(x => x.AssigneeName is not null);
    }
}
