using FluentValidation;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Features.Cards.CreateCard;

public sealed record CreateCardCommand(int SprintId, string Text, string ColumnType, string CardType, string? AssigneeName);

public sealed class CreateCardCommandValidator : Validator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Card.TextMaxLength);
        RuleFor(x => x.ColumnType).Cascade(CascadeMode.Stop)
            .NotEmpty().Must(IsEnumName<ColumnType>).WithMessage("Invalid column type.");
        RuleFor(x => x.CardType).Cascade(CascadeMode.Stop)
            .NotEmpty().Must(IsEnumName<CardType>).WithMessage("Invalid card type.");
        RuleFor(x => x.AssigneeName).MaximumLength(Card.AssigneeNameMaxLength).When(x => x.AssigneeName is not null);
    }

    private static bool IsEnumName<TEnum>(string? value) where TEnum : struct, Enum
        => value is not null && Enum.GetNames<TEnum>().Contains(value);
}
