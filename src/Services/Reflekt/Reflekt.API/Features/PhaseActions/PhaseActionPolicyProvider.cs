using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.PhaseActions;

public sealed class PhaseActionPolicyProvider(IServiceScopeFactory scopeFactory)
{
    private readonly Lock _gate = new();
    private Task<PhaseActionPolicy>? _load;

    public Task<PhaseActionPolicy> GetAsync()
    {
        lock (_gate)
        {
            if (_load is null || _load.IsFaulted || _load.IsCanceled)
                _load = Task.Run(LoadAsync);
            return _load;
        }
    }

    private async Task<PhaseActionPolicy> LoadAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PhaseActionRepository>();
        var rows = await repository.GetAllAsync(CancellationToken.None);
        return new PhaseActionPolicy(rows);
    }
}
