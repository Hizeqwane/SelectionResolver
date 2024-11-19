namespace SelectionResolver;

/// <summary>
/// Сервис получения экземпляра по условию
/// </summary>
public interface ISelectionResolver<in TIn, out TService> where TService : class
{
    /// <summary>
    /// Получить сервис по значению 
    /// </summary>
    TService Get(TIn input, IServiceProvider serviceProvider);

    /// <summary>
    /// Есть ли экземпляр по значению
    /// </summary>
    bool HasFor(TIn input, IServiceProvider serviceProvider);
}