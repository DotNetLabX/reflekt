using FluentValidation;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.Sprints.CreateSprint;

public sealed record CreateSprintCommand(
    int TeamId,
    string Name,
    int? VotesPerParticipant,
    string? Column1Name,
    string? Column1Color,
    string? Column2Name,
    string? Column2Color,
    string? Column3Name,
    string? Column3Color,
    int? WritingTimerDefault,
    int? VotingTimerDefault,
    int? DiscussingTimerDefault);

public sealed class CreateSprintCommandValidator : Validator<CreateSprintCommand>
{
    public CreateSprintCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Sprint.NameMaxLength);
        RuleFor(x => x.VotesPerParticipant).GreaterThanOrEqualTo(Sprint.MinVotesPerParticipant).When(x => x.VotesPerParticipant.HasValue);
        RuleFor(x => x.Column1Name).MaximumLength(Sprint.ColumnNameMaxLength).NotEmpty().When(x => x.Column1Name is not null);
        RuleFor(x => x.Column2Name).MaximumLength(Sprint.ColumnNameMaxLength).NotEmpty().When(x => x.Column2Name is not null);
        RuleFor(x => x.Column3Name).MaximumLength(Sprint.ColumnNameMaxLength).NotEmpty().When(x => x.Column3Name is not null);
        RuleFor(x => x.Column1Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column1Color must be one of the allowed palette values.");
        RuleFor(x => x.Column2Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column2Color must be one of the allowed palette values.");
        RuleFor(x => x.Column3Color).Must(v => v is null || ColumnColor.IsValid(v)).WithMessage("Column3Color must be one of the allowed palette values.");
        RuleFor(x => x.WritingTimerDefault).InclusiveBetween(Sprint.WritingTimerMinMinutes, Sprint.WritingTimerMaxMinutes).When(x => x.WritingTimerDefault.HasValue);
        RuleFor(x => x.VotingTimerDefault).InclusiveBetween(Sprint.VotingTimerMinMinutes, Sprint.VotingTimerMaxMinutes).When(x => x.VotingTimerDefault.HasValue);
        RuleFor(x => x.DiscussingTimerDefault).InclusiveBetween(Sprint.DiscussingTimerMinMinutes, Sprint.DiscussingTimerMaxMinutes).When(x => x.DiscussingTimerDefault.HasValue);
    }
}
