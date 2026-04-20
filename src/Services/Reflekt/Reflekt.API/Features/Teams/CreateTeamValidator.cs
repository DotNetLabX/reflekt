using FastEndpoints;
using FluentValidation;

namespace Reflekt.API.Features.Teams;

public sealed class CreateTeamValidator : Validator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
