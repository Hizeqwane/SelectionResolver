using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SelectionResolver;

/// <inheritdoc />
public class SelectionDictionary<TInterface, TKey>(
    DictSetter<TInterface, TKey> dictSetter)
    : ISelectionDictionary<TInterface, TKey>
    where TInterface : class where TKey : notnull
{
    private ConcurrentDictionary<TKey, List<Type>> _dict;

    /// <inheritdoc />
    public IReadOnlyDictionary<TKey, List<Type>> Get(IServiceProvider serviceProvider) => 
        _dict ??= Set(serviceProvider);

    private ConcurrentDictionary<TKey, List<Type>> Set(IServiceProvider serviceProvider)
    {
        var services = serviceProvider.GetServices<TInterface>();

        var dict = new ConcurrentDictionary<TKey, List<Type>>();

        foreach (var service in services)
        {
            var serviceType = service.GetType();
            var key = dictSetter(service);
            if (dict.TryGetValue(key, out var registeredService))
            {
                registeredService.Add(serviceType);
            }
            
            dict.AddOrUpdate(key, _ => [serviceType], (_, sVal) => sVal);
        }

        return dict;
    }
}