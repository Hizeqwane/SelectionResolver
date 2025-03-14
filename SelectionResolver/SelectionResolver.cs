namespace SelectionResolver;

/// <summary>
/// Получить ключ по экземпляру
/// </summary>
public delegate TIn DictSetter<in TInterface, out TIn>(TInterface input);

/// <summary>
/// Отвечает ли TIn ключу TKey
/// </summary>
public delegate bool KeySelector<in TInterface, in TKey, in TIn>(TKey key, TIn input);

/// <inheritdoc />
public class SelectionResolver<TInterface, TKey, TIn>(
    KeySelector<TInterface, TKey, TIn> keySelector,
    ISelectionDictionary<TInterface, TKey> selectionDictionary,
    IServiceProvider serviceProvider)
    : ISelectionResolver<TIn, TInterface>
    where TInterface : class where TKey : notnull
{
    /// <summary>
    /// Базовый keySelector - использует метод Equals
    /// </summary>
    public static KeySelector<TInterface, TKey, TIn> DefaultKeySelect => (key, input) => key.Equals(input);
    
    /// <inheritdoc />
    public TInterface Get(TIn input)
    {
        var founded = GetFoundedTypeListFromDict(input)
            .FirstOrDefault();

        return founded.Value != null 
            ? (TInterface)serviceProvider.GetService(founded.Value.FirstOrDefault())
            : null;
    }

    /// <inheritdoc />
    public IEnumerable<TInterface> GetMulti(TIn input)
    {
        var founded = GetFoundedTypeListFromDict(input);
        
        return founded.SelectMany(s => s.Value).Select(s => (TInterface)serviceProvider.GetService(s));
    }

    /// <inheritdoc />
    public bool HasFor(TIn input) => 
        selectionDictionary
            .Get(serviceProvider).
            Any(s => keySelector(s.Key, input));
    
    private IEnumerable<KeyValuePair<TKey, List<Type>>> GetFoundedTypeListFromDict(TIn input) =>
        selectionDictionary
            .Get(serviceProvider)
            .Where(s => keySelector(s.Key, input));
}