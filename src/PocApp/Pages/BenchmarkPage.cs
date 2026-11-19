using System.Diagnostics;
using System.Globalization;
using Syncfusion.Windows.Forms.Chart;

namespace PocApp.Pages;

public sealed record BenchmarkConfig(int Series, int PointsPerSecond, int Window, ChartSeriesType Type, bool ImprovePerformance);

public sealed record BenchmarkResult(BenchmarkConfig Config, double Seconds, double UiFps, double PointsPerSecondAchieved, double CpuPercent, double WorkingSetMb);

/// <summary>
/// Streams synthetic points into a ChartControl at a chosen rate and measures how the UI copes.
/// Mode is rolling strip only (oldest point removed once the window is full); sweep mode is not implemented.
/// </summary>
public sealed class BenchmarkPage : UserControl
{
    public const string CsvHeader = "timestamp,series,points_per_sec_per_series,window,type,improve_perf,seconds,ui_fps,points_per_sec_achieved_total,cpu_percent,working_set_mb";

    private readonly ChartControl _chart = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _series = Combo("1", "4", "8");
    private readonly ComboBox _rate = Combo("10", "100", "1000", "10000");
    private readonly ComboBox _type = Combo("Line", "StepLine", "Scatter");
    private readonly NumericUpDown _window = new() { Minimum = 100, Maximum = 100000, Value = 2000, Increment = 500, Width = 80 };
    private readonly CheckBox _improve = new() { Text = "ImprovePerformance", AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
    private readonly Button _run = new() { Text = "Run 10 s", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, Margin = new Padding(12, 6, 0, 0), Text = "Idle" };

    public BenchmarkPage()
    {
        _series.SelectedIndex = 1;
        _rate.SelectedIndex = 1;
        _type.SelectedIndex = 0;
        _run.Click += async (_, _) => await RunFromUiAsync();

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 6, 8, 0) };
        bar.Controls.AddRange([Caption("Series"), _series, Caption("Points/s per series"), _rate, Caption("Window"), _window,
            Caption("Type"), _type, _improve, _run, _status]);

        _chart.Legend.Visible = false;
        Controls.Add(_chart);
        Controls.Add(bar);
    }

    private static ComboBox Combo(params string[] items)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
        c.Items.AddRange(items);
        return c;
    }

    private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(8, 6, 4, 0) };

    private async Task RunFromUiAsync()
    {
        _run.Enabled = false;
        var config = new BenchmarkConfig(
            int.Parse(_series.Text, CultureInfo.InvariantCulture),
            int.Parse(_rate.Text, CultureInfo.InvariantCulture),
            (int)_window.Value,
            Enum.Parse<ChartSeriesType>(_type.Text),
            _improve.Checked);
        _status.Text = "Running...";
        var r = await RunAsync(config, 10);
        _status.Text = $"{r.UiFps:0.#} fps | {r.PointsPerSecondAchieved:0} pts/s | CPU {r.CpuPercent:0}% | {r.WorkingSetMb:0} MB";
        _run.Enabled = true;
    }

    /// <summary>Runs one configuration for <paramref name="seconds"/> and returns the measurements. Must be called on the UI thread.</summary>
    public async Task<BenchmarkResult> RunAsync(BenchmarkConfig cfg, int seconds)
    {
        _chart.Series.Clear();
        _chart.ImprovePerformance = cfg.ImprovePerformance;
        _chart.PrimaryYAxis.Range = new MinMaxInfo(-1.5, 1.5, 0.5);
        var series = new List<ChartSeries>();
        for (var i = 0; i < cfg.Series; i++)
        {
            var s = new ChartSeries($"S{i + 1}", cfg.Type);
            s.Style.Border.Width = 1;
            series.Add(s);
            _chart.Series.Add(s);
        }

        var paints = 0;
        void OnPaint(object? sender, PaintEventArgs e) => paints++;
        _chart.Paint += OnPaint;

        var proc = Process.GetCurrentProcess();
        var cpuStart = proc.TotalProcessorTime;
        var wall = Stopwatch.StartNew();
        var last = TimeSpan.Zero;
        double carry = 0;
        long produced = 0;
        long index = 0;

        while (wall.Elapsed.TotalSeconds < seconds)
        {
            var now = wall.Elapsed;
            carry += cfg.PointsPerSecond * (now - last).TotalSeconds;
            last = now;
            var n = (int)carry;
            carry -= n;

            if (n > 0)
            {
                _chart.BeginUpdate();
                for (var k = 0; k < n; k++)
                {
                    var x = (index + k) / (double)cfg.PointsPerSecond;
                    for (var si = 0; si < series.Count; si++)
                        series[si].Points.Add(x, Math.Sin(x * 2 + si));
                }
                foreach (var s in series)
                    while (s.Points.Count > cfg.Window) s.Points.RemoveAt(0);
                _chart.EndUpdate();
                index += n;
                produced += (long)n * series.Count;
            }

            await Task.Delay(15); // yields to the message loop so painting can happen
        }

        wall.Stop();
        _chart.Paint -= OnPaint;
        proc.Refresh();
        var cpu = (proc.TotalProcessorTime - cpuStart).TotalSeconds / wall.Elapsed.TotalSeconds / Environment.ProcessorCount * 100;
        return new BenchmarkResult(cfg, wall.Elapsed.TotalSeconds, paints / wall.Elapsed.TotalSeconds,
            produced / wall.Elapsed.TotalSeconds, cpu, proc.WorkingSet64 / 1024.0 / 1024.0);
    }

    public static string ToCsvLine(BenchmarkResult r) => string.Join(',',
        DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
        r.Config.Series, r.Config.PointsPerSecond, r.Config.Window, r.Config.Type, r.Config.ImprovePerformance,
        r.Seconds.ToString("0.0", CultureInfo.InvariantCulture),
        r.UiFps.ToString("0.0", CultureInfo.InvariantCulture),
        r.PointsPerSecondAchieved.ToString("0", CultureInfo.InvariantCulture),
        r.CpuPercent.ToString("0.0", CultureInfo.InvariantCulture),
        r.WorkingSetMb.ToString("0", CultureInfo.InvariantCulture));
}
