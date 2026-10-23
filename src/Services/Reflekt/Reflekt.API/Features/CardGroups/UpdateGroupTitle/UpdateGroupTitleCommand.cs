using FluentValidation;
using Reflekt.Domain.CardGroups;

namespace Reflekt.API.Features.CardGroups.UpdateGroupTitle;

public sealed record UpdateGroupTitleRequest(int SprintId, int GroupId, string? Title);

public sealed class UpdateGroupTitleRequestValidator : Validator<UpdateGroupTitleRequest>
{
    public UpdateGroupTitleRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(CardGroup.TitleMaxLength).When(x => x.Title is not null);
    }
}
