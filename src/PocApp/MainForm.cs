using PocApp.Infrastructure;
using PocApp.Pages;

namespace PocApp;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "iocomp to Syncfusion WinForms - POC";
        Width = 1100;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        foreach (var category in ControlCatalog.Categories)
        {
            var count = ControlCatalog.All.Count(e => e.Category == category);
            var tab = new TabPage($"{category} ({count})");
            tab.Controls.Add(new CategoryPage(category) { Dock = DockStyle.Fill });
            tabs.TabPages.Add(tab);
        }

        var benchTab = new TabPage("Benchmark");
        Benchmark = new BenchmarkPage { Dock = DockStyle.Fill };
        benchTab.Controls.Add(Benchmark);
        tabs.TabPages.Add(benchTab);

        var registerTab = new TabPage("Gap register");
        registerTab.Controls.Add(new GapRegisterPage { Dock = DockStyle.Fill });
        tabs.TabPages.Add(registerTab);

        Controls.Add(tabs);

        SignalSimulator.Shared.Start();
        FormClosed += (_, _) => SignalSimulator.Shared.Stop();

        Tabs = tabs;
    }

    private TabControl Tabs { get; }

    public BenchmarkPage Benchmark { get; }

    /// <summary>Runs the series x rate matrix, appends each result to a CSV, then closes. Used for the documented benchmark numbers.</summary>
    public void RunBenchmarkMatrix(string csvPath, int secondsPerRun, bool improvePerformance = false)
    {
        Shown += async (_, _) =>
        {
            try
            {
                SignalSimulator.Shared.Stop(); // keep the other tabs' live updates out of the measurement
                Tabs.SelectedTab = Tabs.TabPages.Cast<TabPage>().First(t => t.Text == "Benchmark");
                if (!File.Exists(csvPath)) File.WriteAllText(csvPath, BenchmarkPage.CsvHeader + Environment.NewLine);
                foreach (var series in new[] { 1, 4, 8 })
                foreach (var rate in new[] { 10, 100, 1000, 10000 })
                {
                    var r = await Benchmark.RunAsync(new BenchmarkConfig(series, rate, 2000, Syncfusion.Windows.Forms.Chart.ChartSeriesType.Line, improvePerformance), secondsPerRun);
                    File.AppendAllText(csvPath, BenchmarkPage.ToCsvLine(r) + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(csvPath + ".error.txt", ex.ToString());
            }
            finally
            {
                Close();
            }
        };
    }

    /// <summary>
    /// Verification aid: renders every tab to a PNG in <paramref name="folder"/>, then runs <paramref name="after"/> and closes.
    /// Expects the simulator to be frozen (see Program) so the images are repeatable.
    /// </summary>
    public void SaveSnapshots(string folder, System.Action? after = null)
    {
        Directory.CreateDirectory(folder);
        Shown += async (_, _) =>
        {
            try
            {
                // 188 fixed ticks = 9.4 s of signal: charts have history, Step is on (lit LEDs, running motor),
                // and Sine is ~51 (below the 85 alarm threshold), so the pictures show normal operation.
                SignalSimulator.Shared.StepFrozen(188);
                await Task.Delay(800);
                foreach (TabPage page in Tabs.TabPages)
                {
                    Tabs.SelectedTab = page;
                    SignalSimulator.Shared.Pulse();
                    page.Refresh();
                    await Task.Delay(500);
                    var name = new string(page.Text.Where(char.IsLetterOrDigit).ToArray());
                    var cards = page.Controls[0].Controls.OfType<FlowLayoutPanel>().FirstOrDefault()?.Controls.OfType<ControlCard>().ToList();
                    if (cards is null || cards.Count == 0)
                    {
                        using var single = new Bitmap(page.Width, page.Height);
                        page.DrawToBitmap(single, new Rectangle(0, 0, page.Width, page.Height));
                        single.Save(Path.Combine(folder, name + ".png"));
                        continue;
                    }

                    // Render every card (not just the visible viewport) and stack them in one image.
                    using var full = new Bitmap(cards.Max(c => c.Width), cards.Sum(c => c.Height + 4));
                    using var g = Graphics.FromImage(full);
                    g.Clear(Color.WhiteSmoke);
                    var y = 0;
                    foreach (var card in cards)
                    {
                        using var one = new Bitmap(card.Width, card.Height);
                        card.DrawToBitmap(one, new Rectangle(0, 0, card.Width, card.Height));
                        g.DrawImage(one, 0, y);
                        y += card.Height + 4;
                    }
                    full.Save(Path.Combine(folder, name + ".png"));
                }
                after?.Invoke();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(folder, "ERROR.txt"), ex.ToString());
                Environment.ExitCode = 2;
            }
            finally
            {
                Close();
            }
        };
    }
}
