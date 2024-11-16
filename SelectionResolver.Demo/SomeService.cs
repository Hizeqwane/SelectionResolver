using SelectionResolver.Demo.Services;

namespace SelectionResolver.Demo;

public class SomeService(
    ISelectionResolver<int, IService> selectionResolver)
{
    public void Handle()
    {
        var serviceWith5TypeId = selectionResolver.Get(5);
    }
}