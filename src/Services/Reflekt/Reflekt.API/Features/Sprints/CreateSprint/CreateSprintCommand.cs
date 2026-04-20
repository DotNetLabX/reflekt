using FluentValidation;

namespace Reflekt.API.Features.Sprints.CreateSprint;

public sealed record CreateSprintCommand(int TeamId, string Name, int? VotesPerParticipant);

public sealed class CreateSprintCommandValidator : Validator<CreateSprintCommand>
{
    public CreateSprintCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VotesPerParticipant).GreaterThanOrEqualTo(1).When(x => x.VotesPerParticipant.HasValue);
    }
}
