using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SelectionResolver;

public delegate TIn DictSetter<in TInterface, out TIn>(TInterface input);

public delegate bool KeySelector<in TInterface, in TKey, in TIn>(TKey key, TIn input);

/// <inheritdoc />
public class SelectionResolver<TInterface, TKey, TIn>(
    DictSetter<TInterface, TKey> dictSetter,
    KeySelector<TInterface, TKey, TIn> keySelector,
    IServiceProvider serviceProvider)
    : ISelectionResolver<TIn, TInterface>
    where TInterface : class where TKey : notnull
{
    private ConcurrentDictionary<TKey, Type> _types;

    public static KeySelector<TInterface, TKey, TIn> DefaultKeySelect => (key, input) => key.Equals(input);
    
    /// <inheritdoc />
    public TInterface Get(TIn input)
    {
        _types ??= Set();
        var founded = _types.FirstOrDefault(s => keySelector(s.Key, input));
        return founded.Value != null 
            ? (TInterface)serviceProvider.GetService(founded.Value)
            : null;
    }

    private ConcurrentDictionary<TKey, Type> Set()
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