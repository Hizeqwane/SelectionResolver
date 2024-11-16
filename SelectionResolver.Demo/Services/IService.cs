using SelectionResolver.Demo.Models;

namespace SelectionResolver.Demo.Services;

/// <summary>
/// Сервис
/// </summary>
public interface IService
{
    /// <summary>
    /// Тип
    /// </summary>
    ServiceType Type { get; }

    /// <summary>
    /// Метод
    /// </summary>
    void Handle();
}