using FluentValidation;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

public sealed record CreateGroupRequest(int SprintId, IReadOnlyList<int> CardIds, string? Title);

public sealed class CreateGroupRequestValidator : Validator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.CardIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Card ids must be distinct.");
    }
}
