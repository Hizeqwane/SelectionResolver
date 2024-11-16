using SelectionResolver.Demo.Models;

namespace SelectionResolver.Demo.Services.Implementations;

/// <inheritdoc />
public abstract class BaseService(int id) : IService
{
    /// <inheritdoc />
    public ServiceType Type => new (id);
    
    /// <inheritdoc />
    public void Handle() => 
        Console.WriteLine(nameof(IService.Handle) + ": " + Type.TypeId);
}