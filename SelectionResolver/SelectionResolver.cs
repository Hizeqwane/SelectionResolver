namespace SelectionResolver;

public delegate TIn DictSetter<in TInterface, out TIn>(TInterface input);

public delegate bool KeySelector<in TInterface, in TKey, in TIn>(TKey key, TIn input);

/// <inheritdoc />
public class SelectionResolver<TInterface, TKey, TIn>(
    KeySelector<TInterface, TKey, TIn> keySelector,
    ISelectionDictionary<TKey> selectionDictionary,
    IServiceProvider serviceProvider)
    : ISelectionResolver<TIn, TInterface>
    where TInterface : class where TKey : notnull
{
    public static KeySelector<TInterface, TKey, TIn> DefaultKeySelect => (key, input) => key.Equals(input);
    
    /// <inheritdoc />
    public TInterface Get(TIn input)
    {
        var founded = selectionDictionary
            .Get(serviceProvider)
            .FirstOrDefault(s => keySelector(s.Key, input));
        
        return founded.Value != null 
            ? (TInterface)serviceProvider.GetService(founded.Value)
            : null;
    }

    /// <inheritdoc />
    public bool HasFor(TIn input) => 
        selectionDictionary
            .Get(serviceProvider).
            Any(s => keySelector(s.Key, input));
}