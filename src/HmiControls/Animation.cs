namespace HmiControls;

/// <summary>Process-wide switch for the controls' own timer animations (flow arrows, rotors, drum roll).</summary>
public static class Animation
{
    /// <summary>When false, animated controls draw a fixed frame and value changes apply immediately. Used for repeatable snapshots.</summary>
    public static bool Enabled { get; set; } = true;
}
