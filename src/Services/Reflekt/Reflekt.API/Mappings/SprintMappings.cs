using Mapster;
using Reflekt.API.Features.Sprints;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Mappings;

public class SprintMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Sprint, SprintResponse>()
            .Map(d => d.State, s => s.State.ToString())
            .Map(d => d.CurrentPhase, s => s.CurrentPhase.ToString());
    }
}
