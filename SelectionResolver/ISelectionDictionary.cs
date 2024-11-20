namespace SelectionResolver;

/// <summary>
/// Хранение справочника типов
/// </summary>
public interface ISelectionDictionary<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Получить справочник
    /// </summary>
    IReadOnlyDictionary<TKey, Type> Get(IServiceProvider serviceProvider);
}