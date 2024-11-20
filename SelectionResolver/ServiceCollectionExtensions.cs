using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SelectionResolver;

/// <summary>
/// Регистрация
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация ISelectionResolver
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="assembly">Сборка для поиска реализаций IService</param>
    /// <param name="dictSetter">Делегат установки ключа</param>
    /// <param name="keySelector">Делегат поиска реализации</param>
    /// <typeparam name="TInterface">Интерфейс множественной регистрации</typeparam>
    /// <typeparam name="TKey">Ключ</typeparam>
    /// <typeparam name="TIn">Параметр для поиска имплементации IInterface</typeparam>
    public static IServiceCollection AddSelectionResolver<TInterface, TKey, TIn>(
        this IServiceCollection services,
        Assembly assembly,
        DictSetter<TInterface, TKey> dictSetter,
        KeySelector<TInterface, TKey, TIn> keySelector)
    where TInterface : class =>
        services
            .AddAllServicesByInterfaceTypeAndImplementation(typeof(TInterface), assembly)
            .AddSingleton(_ => dictSetter)
            .AddSingleton(_ => keySelector)
            .AddSingleton<ISelectionDictionary<TKey>, SelectionDictionary<TInterface, TKey>>()
            .AddScoped<ISelectionResolver<TIn, TInterface>, SelectionResolver<TInterface, TKey, TIn>>();
    
    /// <summary>
    /// Регистрация ISelectionResolver (используется делегат поиска по умолчанию)
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="assembly">Сборка для поиска реализаций IService</param>
    /// <param name="dictSetter">Делегат установки ключа</param>
    /// <typeparam name="TInterface">Интерфейс множественной регистрации</typeparam>
    /// <typeparam name="TKey">Ключ</typeparam>
    /// <typeparam name="TIn">Параметр для поиска имплементации IInterface</typeparam>
    public static IServiceCollection AddSelectionResolver<TInterface, TKey, TIn>(
        this IServiceCollection services,
        Assembly assembly,
        DictSetter<TInterface, TKey> dictSetter)
        where TInterface : class =>
        services.AddSelectionResolver
            (
                assembly,
                dictSetter,
                SelectionResolver<TInterface, TKey, TIn>.DefaultKeySelect
            );

    public static IServiceCollection AddAllServicesByInterfaceTypeOnlyImplementations(
        this IServiceCollection services,
        Type interfaceType,
        Assembly assembly)
    {
        var typeOfInterface = interfaceType;
        if (!typeOfInterface.IsInterface)
            throw new ArgumentException($"{typeOfInterface.Name} не является интерфейсом.");
        
        var handlers = assembly
            .ExportedTypes
            .Where
            (
                s => !s.IsAbstract && 
                     s.GetInterfaces()
                         .Any(i => i == typeOfInterface)
            );

        return handlers.Aggregate
        (
            services,
            (current, next) => current.AddScoped(next)
        );
    }
    
    public static IServiceCollection AddAllServicesByInterfaceType(
        this IServiceCollection services,
        Type interfaceType,
        Assembly assembly)
    {
        var handlers = GetInterfaceAndImplementationTuple(interfaceType, assembly);

        return handlers.Aggregate
        (
            services,
            (current, next) => current.AddScoped(next.InterfaceImpType, next.Type)
        );
    }
    
    public static IServiceCollection AddAllServicesByInterfaceTypeAndImplementation(
        this IServiceCollection services,
        Type interfaceType,
        Assembly assembly) =>
        interfaceType.
            GetInterfaceAndImplementationTuple(assembly)
            .Aggregate
            (
                services,
                (current, next) => current.AddScoped(next.InterfaceImpType, next.Type)
                    .AddScoped(next.Type)
            );

    public static IEnumerable<(Type Type, Type InterfaceImpType)> GetInterfaceAndImplementationTuple(
        this Type interfaceType,
        Assembly assembly)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType.Name} не является интерфейсом.");
        
        return assembly
            .ExportedTypes
            .Select(s => (s, s.GetInterfaces().FirstOrDefault(i => i.GUID == interfaceType.GUID)))
            .Where(t => !t.s.IsAbstract && t.Item2 != null);
    }
}