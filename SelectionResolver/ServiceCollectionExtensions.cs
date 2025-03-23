using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <param name="assemblies">Сборки для поиска реализаций IService</param>
    /// <param name="dictSetter">Делегат установки ключа</param>
    /// <param name="keySelector">Делегат поиска реализации</param>
    /// <param name="serviceLifetime">Время жизни для сервисов</param>
    /// <typeparam name="TInterface">Интерфейс множественной регистрации</typeparam>
    /// <typeparam name="TKey">Ключ</typeparam>
    /// <typeparam name="TIn">Параметр для поиска имплементации IInterface</typeparam>
    public static IServiceCollection AddSelectionResolver<TInterface, TKey, TIn>(
        this IServiceCollection services,
        DictSetter<TInterface, TKey> dictSetter,
        KeySelector<TInterface, TKey, TIn> keySelector,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    where TInterface : class =>
        services
            .AddAllServicesByInterfaceTypeAndImplementation(typeof(TInterface), serviceLifetime, assemblies)
            .AddSingleton(_ => dictSetter)
            .AddSingleton(_ => keySelector)
            .AddSingleton<ISelectionDictionary<TInterface, TKey>, SelectionDictionary<TInterface, TKey>>()
            .AddService<ISelectionResolver<TIn, TInterface>, SelectionResolver<TInterface, TKey, TIn>>(serviceLifetime);

    /// <summary>
    /// Регистрация ISelectionResolver (используется делегат поиска по умолчанию)
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="serviceLifetime">Время жизни для сервисов</param>
    /// <param name="assemblies">Сборки для поиска реализаций IService</param>
    /// <param name="dictSetter">Делегат установки ключа</param>
    /// <typeparam name="TInterface">Интерфейс множественной регистрации</typeparam>
    /// <typeparam name="TKey">Ключ</typeparam>
    /// <typeparam name="TIn">Параметр для поиска имплементации IInterface</typeparam>
    public static IServiceCollection AddSelectionResolver<TInterface, TKey, TIn>(
        this IServiceCollection services,
        DictSetter<TInterface, TKey> dictSetter,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
        where TInterface : class =>
        services.AddSelectionResolver
            (
                dictSetter,
                SelectionResolver<TInterface, TKey, TIn>.DefaultKeySelect,
                serviceLifetime,
                assemblies
            );
    
    public static IServiceCollection AddAllServicesByInterfaceType(
        this IServiceCollection services,
        Type interfaceType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    {
        var handlers = GetInterfaceAndImplementationTuple(interfaceType, assemblies);

        return handlers.Aggregate
        (
            services,
            (current, next) => current
                .AddService(new ServiceDescriptor(next.InterfaceImpType, next.Type, serviceLifetime))
        );
    }
    
    public static IServiceCollection AddAllServicesByInterfaceTypeAndImplementation(
        this IServiceCollection services,
        Type interfaceType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies) =>
        interfaceType.
            GetInterfaceAndImplementationTuple(assemblies)
            .Aggregate
            (
                services,
                (current, next) => current
                    .AddService(new ServiceDescriptor(next.InterfaceImpType, next.Type, serviceLifetime))
                    .AddService(new ServiceDescriptor(next.Type, next.Type, serviceLifetime)));

    public static IEnumerable<(Type Type, Type InterfaceImpType)> GetInterfaceAndImplementationTuple(
        this Type interfaceType,
        params Assembly[] assemblies)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType.Name} не является интерфейсом.");
        
        return assemblies
            .SelectMany(s => s.ExportedTypes)
            .Select(s => (s, s.GetInterfaces().FirstOrDefault(i => i.GUID == interfaceType.GUID)))
            .Where(t => !t.s.IsAbstract && t.Item2 != null);
    }

    private static IServiceCollection AddService(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor) =>
        ServiceCollectionDescriptorExtensions
            .Add(services, serviceDescriptor);
    
    private static IServiceCollection AddService<TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        switch (serviceLifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped<TService, TImplementation>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<TService, TImplementation>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<TService, TImplementation>();
                break;
        }

        return services;
    }
}