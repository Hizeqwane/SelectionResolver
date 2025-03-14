namespace SelectionResolver;

/// <summary>
/// Хранение справочника типов
/// </summary>
public interface ISelectionDictionary<TInterface, TKey>
    where TInterface : class where TKey : notnull
{
    /// <summary>
    /// Получить справочник
    /// </summary>
    IReadOnlyDictionary<TKey, List<Type>> Get(IServiceProvider serviceProvider);
}