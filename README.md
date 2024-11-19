# Регистрация экземпляротипизированных сервисов

Данный сервис предназначен для случаев, когда необходимо в IServiceProvider`е регистрировать множество классов, имплементирующих один и тот же интерфейс, при этом существует условие выбора конкретной реализации в зависимости от определённого условия, накладываемого на экземпляр.

Функциональность сервиса представлена интерфейсом _ISelectionResolver<TInterface, TKey, TIn>_, где

_TInterface_ - интерфейс, множество имплементация которого необходимо зарегистрировать;

_TKey_ - объект, который будет представлять ключ (однозначное соответствие "ключ - имплементация");

_TIn_ - объект, по которому будем получать имплементацию.

Интерфейс содержит методы:
- _IInterface Get(TIn input, IServiceProvider serviceProvider)_ - получение экземпляра по значению TIn;
- _bool HasFor(TIn input, IServiceProvider serviceProvider)_ - отвечает на вопрос: зарегистрирован ли экземпляр для значения TIn.

Прокидывание _IServiceProvider\`а_ обусловлено разным временем жизни _ISelectionResolver_ и _IServic\`ов_, для которых используется резолвер.

Сам по себе _ISelectionResolver_ использует _ConcurrentDictionary_ для сопоставления "ключ-имплементация" и регистрируется как _Singleton_.

## Регистрация

Для регистрации _ISelectionResolver_ есть два метода:

```
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
        where TInterface : class
        
    ...
    ...
    
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
        where TInterface : class
```
Делегат поиска по умолчанию выглядит следующим образом:
```
public static KeySelector<TInterface, TKey, TIn> DefaultKeySelect => (key, input) => key.Equals(input);
```

При использовании регистрации такого вида отпадает необходимость использовать делегаты регистрации в IServiceProvider, так как такой способ требует актуализации делегата при добавлении новых имплементаций.
Таким образом при добавлении новых имплементаций не требуется вносить изменений в классы регистрации классов (как правило регистрацию сервисов реализуют в _Startup.cs_).

## Пример

Есть интерфейс _IService_ и требуется зарегистрировать множество его реализаций.

```
public record ServiceType(int TypeId);

public interface IService
{
    ServiceType Type { get; }

    void Handle();
}

```

Тогда _TInterface - IService_, _TKey - ServiceType_ (хотя ключом может быть и int - TypeId у _ServiceType_; возьмём _ServiceType_ для рассмотрения более общего случая), а _TIn - int_.

### Использование

```
// Регистрация
...
    services.AddSelectionResolver<IService, ServiceType, int>(
        Assembly.GetExecutingAssembly(),
        iService => iService.Type,
        (type, input) => type.TypeId == input);
...
```
_Примечание: данная регистрация предполагает, что все реализации IService будут иметь ServiceType с разными TypeId_.

```
// Использование в сервисах
...
public class SomeService(
    ISelectionResolver<int, IService> selectionResolver,
    IServiceProvider serviceProvider)
{
    public void Handle()
    {
        var serviceWith5TypeId = selectionResolver.Get(5, serviceProvider);
    }
}
...
```

## Результаты
В репозитории приводится проект с регистрациями 100/500/1000 сервисов и сравнением времени получения имплементации по условию:
- использование _IEnumerable\<IService\>_, которую предоставляет _IServiceProvider_ для множества имплементаций
- использование _ISelectionResolver\<IService, ServiceType, int\>_

При этом в расчёт не берётся время первого обращения, так как и _IServiceProvider_ после первого обращения кэширует результаты (несмотря на создание scope перед каждым обращением), так и _ISelectionResolver_ при первом обращении создаёт справочник "ключ - имплементация".

100:
```
Without selection resolver
 Max: 00:00:00.0320488
 Mean: 00:00:00.0003895
 Median: 00:00:00.0000688
 
With selection resolver
 Max: 00:00:00.0007627
 Mean: 00:00:00.0000163
 Median: 00:00:00.0000072
```

500:
```
Without selection resolver
 Max: 00:00:00.1999964
 Mean: 00:00:00.0006230
 Median: 00:00:00.0001686
 
With selection resolver
 Max: 00:00:00.0011567
 Mean: 00:00:00.0000298
 Median: 00:00:00.0000269
```

1000:
```
Without selection resolver
 Max: 00:00:00.4572421
 Mean: 00:00:00.0007301
 Median: 00:00:00.0001898
 
With selection resolver
 Max: 00:00:00.0024519
 Mean: 00:00:00.0000461
 Median: 00:00:00.0000488
```
