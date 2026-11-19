namespace PocApp.Infrastructure;

/// <summary>Shared signal source. Live controls subscribe to <see cref="Tick"/> and read the signal properties.</summary>
public sealed class SignalSimulator : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Random _random = new(42);
    private double _t;

    public static SignalSimulator Shared { get; } = new();

    public event EventHandler? Tick;

    public double Sine { get; private set; }      // 0..100
    public double Noise { get; private set; }     // 0..100
    public double Ramp { get; private set; }      // 0..100, sawtooth
    public bool Step { get; private set; }        // toggles every 2 s

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

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void Advance()
    {
        _t += _timer.Interval / 1000.0;
        Sine = 50 + 50 * Math.Sin(_t);
        Noise = Math.Clamp(Sine + (_random.NextDouble() - 0.5) * 20, 0, 100);
        Ramp = _t * 10 % 100;
        Step = (int)(_t / 2) % 2 == 0;
        Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _timer.Dispose();
}
