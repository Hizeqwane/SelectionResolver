namespace SelectionResolver.Demo.Models;

/// <summary>
/// Статистика
/// </summary>
public record Stat(TimeSpan Max, TimeSpan Mean, TimeSpan Median)
{
    public override string ToString()
    {
        return
@$" {nameof(Max)}: {Max}
 {nameof(Mean)}: {Mean}
 {nameof(Median)}: {Median}";
    }
}