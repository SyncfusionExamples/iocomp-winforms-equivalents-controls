namespace PocApp.Infrastructure;

/// <summary>Shared signal source. Live controls subscribe to <see cref="Tick"/> and read the signal properties.</summary>
public sealed class SignalSimulator : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Random _random = new(42);
    private double _t;
    private DateTime? _frozenNow;

    public static SignalSimulator Shared { get; } = new();

    public event EventHandler? Tick;

    public double Sine { get; private set; }      // 0..100
    public double Noise { get; private set; }     // 0..100
    public double Ramp { get; private set; }      // 0..100, sawtooth
    public bool Step { get; private set; }        // toggles every 2 s

    /// <summary>Wall-clock time for the clock cards; fixed while frozen so snapshots are repeatable.</summary>
    public DateTime Now => _frozenNow ?? DateTime.Now;

    public bool IsFrozen => _frozenNow is not null;

    public int IntervalMs
    {
        get => _timer.Interval;
        set => _timer.Interval = Math.Max(1, value);
    }

    private SignalSimulator()
    {
        _timer.Interval = 50;
        _timer.Tick += (_, _) => Advance();
    }

    public void Start()
    {
        if (!IsFrozen) _timer.Start();
    }

    public void Stop() => _timer.Stop();

    /// <summary>
    /// Snapshot/regression mode: the timer never runs and the clock is fixed. Time only moves through
    /// <see cref="StepFrozen"/>, so the same calls always produce the same values (the noise generator is seeded).
    /// </summary>
    public void Freeze(DateTime now)
    {
        _timer.Stop();
        _frozenNow = now;
        _t = 0;
        Compute();
    }

    /// <summary>Frozen mode: advances <paramref name="ticks"/> fixed 50 ms steps, raising Tick each time (builds chart history).</summary>
    public void StepFrozen(int ticks)
    {
        for (var i = 0; i < ticks; i++) Advance();
    }

    /// <summary>Raises Tick without advancing time (frozen mode), so controls created later pick up the values.</summary>
    public void Pulse() => Tick?.Invoke(this, EventArgs.Empty);

    private void Advance()
    {
        _t += _timer.Interval / 1000.0;
        Compute();
        Tick?.Invoke(this, EventArgs.Empty);
    }

    private void Compute()
    {
        Sine = 50 + 50 * Math.Sin(_t);
        Noise = Math.Clamp(Sine + (_random.NextDouble() - 0.5) * 20, 0, 100);
        Ramp = _t * 10 % 100;
        Step = (int)(_t / 2) % 2 == 0;
    }

    public void Dispose() => _timer.Dispose();
}
