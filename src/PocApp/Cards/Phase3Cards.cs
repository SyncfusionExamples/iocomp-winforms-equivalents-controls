using PocApp.Infrastructure;
using Syncfusion.Drawing;
using Syncfusion.Windows.Forms.Chart;

namespace PocApp.Cards;

/// <summary>Live ChartControl cards for the Plot category (Plot, SpectrumDisplay, PercentBar, PercentPie).</summary>
public static class Phase3Cards
{
    private const int Window = 300;

    public static void Register()
    {
        // Plot (E): default = one rolling line; restyled = dark, mixed series (line + column + bubble) like the iocomp image.
        Phase2Cards.Add("Plot", () => RollingLine(dark: false), () => MixedDark());

        // SpectrumDisplay (A): column series redrawn on each tick.
        Phase2Cards.Add("SpectrumDisplay", Spectrum);

        // PercentBar (E): default = 100% stacked columns; restyled = 100% stacked horizontal bars with a palette.
        Phase2Cards.Add("PercentBar",
            () => Stacked(ChartSeriesType.StackingColumn100, ChartColorPalette.Default),
            () => PercentColumn());

        // PercentPie (A)
        Phase2Cards.Add("PercentPie", Pie);
    }

    private static ChartControl NewChart(bool legend)
    {
        var c = new ChartControl();
        c.Legend.Visible = legend;
        c.Legend.Position = ChartDock.Bottom;
        c.LegendsPlacement = ChartPlacement.Outside; // default placement drew the legend over the plot area
        c.LegendAlignment = ChartAlignment.Center;
        return c;
    }

    private static ChartSeries NewSeries(string name, ChartSeriesType type, Color? color = null)
    {
        var s = new ChartSeries(name, type);
        if (color is Color col)
        {
            s.Style.Interior = new BrushInfo(col);
            s.Style.Border.Color = col;
        }
        return s;
    }

    // -------------------------------------------------------------- Plot

    private static ChartControl RollingLine(bool dark)
    {
        var chart = NewChart(legend: true);
        var sine = NewSeries("Sine", ChartSeriesType.Line, Color.SteelBlue);
        sine.Style.Border.Width = 2;
        chart.Series.Add(sine);
        chart.PrimaryYAxis.Range = new MinMaxInfo(0, 100, 20);
        chart.PrimaryXAxis.ValueType = ChartValueType.Double;
        return Roll(chart, s => (sine, s.Sine));
    }

    private static ChartControl Roll(ChartControl chart, Func<SignalSimulator, (ChartSeries series, double value)> pick)
    {
        double x = 0;
        return Phase2Cards.Live(chart, sim =>
        {
            var (series, value) = pick(sim);
            series.Points.Add(x++, value);
            if (series.Points.Count > Window) series.Points.RemoveAt(0);
        });
    }

    // Restyled to match the iocomp Plot: black plot, white axis lines, dark-green grid, "0.0" labels, legend on top,
    // red line + green columns + shaded blue bubbles, sunken 3D frame. The X axis scrolls a 100-unit window like a strip chart.
    private static Control MixedDark()
    {
        const double span = 100;
        var gridGreen = Color.FromArgb(0, 128, 0);

        var chart = NewChart(legend: true);
        chart.Dock = DockStyle.Fill;
        chart.Legend.Position = ChartDock.Top;
        chart.Legend.ShowBorder = false;
        chart.Legend.BackInterior = new BrushInfo(Color.Black);
        chart.Legend.ForeColor = Color.White;
        chart.Legend.ItemsSize = new Size(14, 14);
        chart.Legend.Spacing = 4;
        chart.Legend.Font = new Font("Segoe UI", 7f);

        var line = NewSeries("Channel 1", ChartSeriesType.Line, Color.Red);
        line.Style.Border.Width = 2;

        var columns = NewSeries("Channel 2", ChartSeriesType.Column, Color.Lime);
        columns.Style.Border.Color = Color.Lime;
        chart.ColumnWidthMode = ChartColumnWidthMode.FixedWidthMode;
        chart.ColumnFixedWidth = 8;

        var bubbles = NewSeries("Channel 3", ChartSeriesType.Bubble, Color.FromArgb(0, 110, 255));
        bubbles.Style.Border.Color = Color.FromArgb(0, 18, 255);
        var bubbleItem = bubbles.ConfigItems.BubbleItem;
        bubbleItem.BubbleType = ChartBubbleType.Circle;
        bubbleItem.EnablePhongStyle = true;
        bubbleItem.MinBounds = new RectangleF(0, 0, 12, 12); // equal min/max keeps every bubble the iocomp size
        bubbleItem.MaxBounds = new RectangleF(0, 0, 12, 12);

        // Added in legend order (Channel 1, 2, 3); ZOrder then draws the columns behind the line, as iocomp does.
        chart.Series.Add(line);
        chart.Series.Add(columns);
        chart.Series.Add(bubbles);
        columns.ZOrder = 0;
        line.ZOrder = 1;
        bubbles.ZOrder = 2;
        // LegendItem is only valid once the series belongs to a chart (NullReferenceException before Series.Add).
        line.LegendItemUseSeriesStyle = false;
        line.LegendItem.Type = ChartLegendItemType.StraightLine;
        line.LegendItem.ItemStyle.Type = ChartLegendItemType.StraightLine;
        line.LegendItem.ItemStyle.RepresentationSize = new Size(14, 3);
        columns.LegendItemUseSeriesStyle = false;
        columns.LegendItem.Type = ChartLegendItemType.Rectangle;
        bubbles.LegendItemUseSeriesStyle = false;
        bubbles.LegendItem.Type = ChartLegendItemType.Circle;

        chart.BackInterior = new BrushInfo(Color.Black);
        chart.ChartArea.BackInterior = new BrushInfo(Color.Black);
        chart.ChartArea.GridBackInterior = new BrushInfo(Color.Black);
        chart.ChartArea.BorderColor = Color.Black;
        chart.ForeColor = Color.White;
        foreach (var axis in new[] { chart.PrimaryXAxis, chart.PrimaryYAxis })
        {
            axis.ForeColor = Color.White;
            axis.LineType.ForeColor = Color.White;
            axis.GridLineType.ForeColor = gridGreen;
            axis.TickColor = Color.White;
            axis.Format = "0.0";
            axis.Font = new Font("Segoe UI", 7f);
            axis.EdgeLabelsDrawingMode = ChartAxisEdgeLabelsDrawingMode.ClippingProtection;
        }
        chart.PrimaryYAxis.Range = new MinMaxInfo(0, 100, 20);
        chart.PrimaryXAxis.ValueType = ChartValueType.Double;
        chart.PrimaryXAxis.Range = new MinMaxInfo(0, span, span / 2);
        chart.PrimaryXAxis.SmallTicksPerInterval = 4;

        // iocomp draws a sunken 3D frame round the whole plot.
        var frame = new Panel { BorderStyle = BorderStyle.Fixed3D, BackColor = Color.Black };
        frame.Controls.Add(chart);

        double x = 0;
        return Phase2Cards.Live(frame, sim =>
        {
            chart.BeginUpdate();
            line.Points.Add(x, 8 + sim.Noise * 0.18);
            var slot = (int)x % 12;
            if (slot == 0) columns.Points.Add(x, 40 + sim.Noise * 0.22);
            if (slot == 6) bubbles.Points.Add(x, new[] { 78 + sim.Noise * 0.16, 1 });
            x++;

            var min = Math.Max(0, x - span);
            foreach (var s in new[] { line, columns, bubbles })
                while (s.Points.Count > 0 && s.Points[0].X < min) s.Points.RemoveAt(0);
            chart.PrimaryXAxis.Range = new MinMaxInfo(min, min + span, span / 2);
            chart.EndUpdate();
        });
    }

    // -------------------------------------------------------------- SpectrumDisplay

    private static ChartControl Spectrum()
    {
        var chart = NewChart(legend: false);
        var bars = NewSeries("Spectrum", ChartSeriesType.Column, Color.LimeGreen);
        chart.Series.Add(bars);
        chart.PrimaryYAxis.Range = new MinMaxInfo(0, 100, 20);
        chart.PrimaryXAxis.Range = new MinMaxInfo(-0.5, 31.5, 8); // default axis ran -10..40 for data 0..31
        chart.BackInterior = new BrushInfo(Color.Black);
        chart.ChartArea.BackInterior = new BrushInfo(Color.Black);
        chart.ChartArea.GridBackInterior = new BrushInfo(Color.Black);
        chart.ForeColor = Color.Gainsboro;
        chart.PrimaryXAxis.ForeColor = Color.Gainsboro;
        chart.PrimaryYAxis.ForeColor = Color.Gainsboro;

        double phase = 0;
        return Phase2Cards.Live(chart, sim =>
        {
            phase += 0.15;
            chart.BeginUpdate();
            bars.Points.Clear();
            for (var i = 0; i < 32; i++)
            {
                // Simulated FFT magnitude: decaying envelope with two moving peaks.
                var env = 90 * Math.Exp(-i / 14.0);
                var peaks = 25 * Math.Abs(Math.Sin(i * 0.5 + phase)) + 15 * Math.Abs(Math.Sin(i * 0.23 - phase));
                bars.Points.Add(i, Math.Clamp(env * 0.5 + peaks, 0, 100));
            }
            chart.EndUpdate();
        });
    }

    // -------------------------------------------------------------- PercentBar / PercentPie

    private static ChartControl Stacked(ChartSeriesType type, ChartColorPalette palette)
    {
        var chart = NewChart(legend: true);
        chart.Palette = palette;
        if (type is ChartSeriesType.StackingBar100 or ChartSeriesType.StackingBar or ChartSeriesType.Bar)
        {
            // Horizontal bar types swap the axes: PrimaryXAxis is the horizontal VALUE axis and the Y axis holds the categories.
            // Leaving X as Category (as for columns) squashed every bar into the right-hand corner.
            chart.PrimaryXAxis.ValueType = ChartValueType.Double;
            chart.PrimaryXAxis.Range = new MinMaxInfo(0, 100, 20);
        }
        else
        {
            chart.PrimaryXAxis.ValueType = ChartValueType.Category;
        }
        string[] cats = ["Q1", "Q2", "Q3", "Q4"];
        double[][] data = [[30, 20, 25, 40], [50, 45, 40, 35], [20, 35, 35, 25]];
        string[] names = ["Normal", "Warning", "Alarm"];
        for (var s = 0; s < data.Length; s++)
        {
            var series = new ChartSeries(names[s], type);
            for (var i = 0; i < cats.Length; i++) series.Points.Add(cats[i], data[s][i]);
            chart.Series.Add(series);
        }
        return chart;
    }

    // iocomp PercentBar: one 100% stacked column (red / yellow / orange, cylinder-shaded), legend on the right, no axes.
    private static ChartControl PercentColumn()
    {
        var chart = NewChart(legend: true);
        chart.Legend.Position = ChartDock.Right;
        chart.Legend.ShowBorder = false;
        chart.Legend.ItemsSize = new Size(16, 16);
        chart.Legend.Font = new Font("Segoe UI", 10f);
        chart.PrimaryXAxis.ValueType = ChartValueType.Category;
        chart.PrimaryXAxis.IsVisible = false;
        chart.PrimaryYAxis.IsVisible = false;
        chart.PrimaryXAxis.DrawGrid = false;
        chart.PrimaryYAxis.DrawGrid = false;
        chart.ChartArea.BorderWidth = 0;
        chart.ChartArea.BorderColor = Color.Transparent;
        chart.ChartArea.BorderStyle = BorderStyle.None;
        var face = new BrushInfo(Color.FromArgb(240, 240, 240));
        chart.BackInterior = chart.ChartArea.BackInterior = chart.ChartArea.GridBackInterior = chart.Legend.BackInterior = face;
        chart.ColumnWidthMode = ChartColumnWidthMode.FixedWidthMode;
        chart.ColumnFixedWidth = 70; // one wide column, like the iocomp bar

        (string Name, double Value, Color Color)[] items = [("Item 3", 80, Color.Orange), ("Item 2", 15, Color.Yellow), ("Item 1", 5, Color.Red)];
        foreach (var (name, value, color) in items)
        {
            var s = NewSeries(name, ChartSeriesType.StackingColumn100, color);
            s.Points.Add("Total", value);
            s.ConfigItems.ColumnItem.ShadingMode = ChartColumnShadingMode.PhongCylinder; // cylinder shading
            chart.Series.Add(s);
        }
        // Series are added bottom-up for stacking, so the legend lists Item 3 first; iocomp lists Item 1 first.
        // No legend-order option was found in 35.2.12, so this difference is recorded in the register.
        return chart;
    }

    private static ChartControl Pie()
    {
        var chart = NewChart(legend: true);
        var pie = new ChartSeries("Share", ChartSeriesType.Pie);
        pie.Points.Add("Normal", 62);
        pie.Points.Add("Warning", 25);
        pie.Points.Add("Alarm", 13);
        pie.Style.DisplayText = true;
        chart.Series.Add(pie);
        return chart;
    }
}
