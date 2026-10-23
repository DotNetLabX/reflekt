using FluentValidation;

namespace Reflekt.API.Features.PreviousActions.SetReviewStatus;

public sealed record SetReviewStatusCommand(int TeamId, int SprintId, int CardId, string ReviewStatus);

public sealed class SetReviewStatusCommandValidator : Validator<SetReviewStatusCommand>
{
    private static readonly HashSet<string> ValidStatuses =
        ["Done", "CarriedOver", "Dropped"];

    public SetReviewStatusCommandValidator()
    {
        RuleFor(x => x.ReviewStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("ReviewStatus must be one of: Done, CarriedOver, Dropped.");
    }
}
