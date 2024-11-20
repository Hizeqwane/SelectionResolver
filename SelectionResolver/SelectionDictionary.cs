using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SelectionResolver;

/// <inheritdoc />
public class SelectionDictionary<TInterface, TKey>(
    DictSetter<TInterface, TKey> dictSetter)
    : ISelectionDictionary<TInterface, TKey>
    where TInterface : class where TKey : notnull
{
    private ConcurrentDictionary<TKey, Type> _dict;
    
    /// <inheritdoc />
    public IReadOnlyDictionary<TKey, Type> Get(IServiceProvider serviceProvider) => 
        (_dict ??= Set(serviceProvider));

    private ConcurrentDictionary<TKey, Type> Set(IServiceProvider serviceProvider)
    {
        var services = serviceProvider.GetServices<TInterface>();

        var dict = new ConcurrentDictionary<TKey, Type>();

        foreach (var service in services)
        {
            var key = dictSetter(service);
            if (dict.ContainsKey(key))
                throw new ApplicationException($"Для значения {key} уже зарегистрирован сервис.");

            dict.AddOrUpdate(key, _ => service.GetType(), (_, sVal) => sVal);
        }

        return dict;
    }
}