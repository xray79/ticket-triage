using Microsoft.Extensions.DependencyInjection;
using Triage.Application.Providers;

namespace Triage.Infrastructure.Providers;

public sealed class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    public const string LocalKey = "local";

    public LlmProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ITriageLlmClient LocalClient => Resolve(LocalKey);

    public ITriageLlmClient Resolve(string providerKey)
    {
        var key = string.IsNullOrWhiteSpace(providerKey) ? LocalKey : providerKey.ToLowerInvariant();
        return _serviceProvider.GetKeyedService<ITriageLlmClient>(key)
            ?? _serviceProvider.GetRequiredKeyedService<ITriageLlmClient>(LocalKey);
    }
}
