namespace PocApp.Infrastructure;

/// <summary>
/// Registry of live-control builders, keyed by iocomp control name.
/// Empty in Phase 1; later phases register builders here so cards show real controls.
/// </summary>
public static class CardFactories
{
    public static readonly Dictionary<string, Func<Control>> Default = new();
    public static readonly Dictionary<string, Func<Control>> Restyled = new();

    public static Func<Control>? DefaultFor(string name) => Default.GetValueOrDefault(name);

    public static Func<Control>? RestyledFor(string name) => Restyled.GetValueOrDefault(name);
}
