using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SelectionResolver.Demo.Models;
using SelectionResolver.Demo.Services;

namespace SelectionResolver.Demo;

public class Tests
{
#pragma warning disable NUnit1032
    private IServiceProvider _serviceProviderWithSelectionResolver;
    
    private IServiceProvider _serviceProviderWithoutSelectionResolver;
#pragma warning restore NUnit1032
    
    [SetUp]
    public void Setup()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        var servicesWithSelectionResolver = new ServiceCollection();
        
        servicesWithSelectionResolver.AddSelectionResolver<IService, ServiceType, int>(
            Assembly.GetExecutingAssembly(),
            iService => iService.Type,
            (type, input) => type.TypeId == input);

        _serviceProviderWithSelectionResolver = servicesWithSelectionResolver.BuildServiceProvider();

        var servicesWithoutSelectionResolver = new ServiceCollection();

        servicesWithoutSelectionResolver
            .AddAllServicesByInterfaceType(typeof(IService), assembly);

        _serviceProviderWithoutSelectionResolver = servicesWithoutSelectionResolver.BuildServiceProvider();
    }

    [Test]
    public void Test1()
    {
        var allServices = typeof(IService)
            .GetInterfaceAndImplementationTuple(Assembly.GetExecutingAssembly())
            .ToList();

        _ = _serviceProviderWithSelectionResolver.GetServices<IService>().ToList();

        var withoutSelectionResolverStat = GetResults
        (
            allServices.Select(s => s.Type).ToList(),
            _serviceProviderWithoutSelectionResolver,
            (provider, id) => (IService)provider
                .GetServices(typeof(IService))
                .ToList()
                .FirstOrDefault(s => ((IService)s!).Type.TypeId == id)!
        );

        Console.WriteLine(@$"Without selection resolver
{withoutSelectionResolverStat}");
        
        var withSelectionResolverStat = GetResults
        (
            allServices.Select(s => s.Type).ToList(),
            _serviceProviderWithSelectionResolver,
            (provider, id) => provider
                .GetRequiredService<ISelectionResolver<int, IService>>()
                .Get(id, _serviceProviderWithSelectionResolver)
        );
        
        Console.WriteLine(@$"With selection resolver
{withSelectionResolverStat}");
    }

    private Stat GetResults(
        List<Type> serviceTypes,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, int, IService> serviceGetter)
    {
        var watch = new Stopwatch();
        
        var ind = 0;
        var maxNotFirstTime = TimeSpan.MinValue;
        var sumNotFirst = TimeSpan.Zero;
        var allTimes = new TimeSpan[serviceTypes.Count - 1];
        
        foreach (var serviceType in serviceTypes)
        {
            ind++;
            
            using var scope = serviceProvider.CreateScope();
            var id = int.Parse(serviceType.Name.Replace("Service", ""));
            
            watch.Restart();
            
            var foundedService = serviceGetter(scope.ServiceProvider, id);
        
            watch.Stop();
        
            Assert.That(foundedService?.GetType(), Is.EqualTo(serviceType));
        
            //Console.WriteLine($"(Without selection resolver) Получение {serviceType.Name}: {watch.Elapsed}");

            var time = watch.Elapsed;
            
            if (ind == 1)
                continue;
            
            if (maxNotFirstTime < time)
                maxNotFirstTime = time;

            sumNotFirst += time;
            allTimes[ind - 2] = time;
        }
        
        return new Stat
        (
            maxNotFirstTime,
            sumNotFirst / (ind - 1),
            allTimes
                .OrderBy(s => s)
                .ToList()[(ind - 1) / 2]
        );
    }
}