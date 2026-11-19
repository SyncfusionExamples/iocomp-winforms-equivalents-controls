using PocApp.Infrastructure;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Diagram;
using Syncfusion.Windows.Forms.Diagram.Controls;
using Syncfusion.Windows.Forms.Tools;
using DRectangle = Syncfusion.Windows.Forms.Diagram.Rectangle;
using Label = System.Windows.Forms.Label;

namespace PocApp.Cards;

/// <summary>Remaining E cards (SwitchToggle, SwitchLed, MatrixButtons, PhonePad, ObjectCanvas) and the stand-ins for two N switches.</summary>
public static class Phase5Cards
{
    public static void Register()
    {
        Phase2Cards.Add("SwitchToggle",
            () => Centered(new ToggleButton { Size = new Size(110, 40) }),
            () => IocompToggle(ToggleLook.Paddle, "Toggle switch"));

        Phase2Cards.Add("SwitchLed", () => LedButton(styled: false), () => IocompToggle(ToggleLook.Led, "LED switch"));

        Phase2Cards.Add("MatrixButtons", () => ButtonGrid(4, 4, i => ((char)('A' + i)).ToString()), () => ButtonGrid(4, 4, i => (i + 1).ToString(), accent: true));

        string[] pad = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#"];
        Phase2Cards.Add("PhonePad", () => ButtonGrid(4, 3, i => pad[i]), () => ButtonGrid(4, 3, i => pad[i], accent: true));

        Phase2Cards.Add("ObjectCanvas", () => DiagramCanvas(), () => DiagramCanvas(rotated: true));

        // N stand-ins. Rocker: ToggleButton + custom renderer. Rocker3Way / Lever: CheckBoxAdv with custom painting.
        Phase2Cards.Add("SwitchRocker", () => IocompToggle(ToggleLook.Rocker, "Rocker switch"));
        Phase2Cards.Add("SwitchRocker3Way", () => WithState(new Rocker3WayBox { CheckState = CheckState.Indeterminate, AccessibleName = "Three-way rocker switch" },
            b => b.CheckState switch { CheckState.Checked => "Up", CheckState.Indeterminate => "Centre", _ => "Down" }));
        Phase2Cards.Add("SwitchLever", () => WithState(new LeverBox { Checked = true, AccessibleName = "Lever switch" }, b => b.Checked ? "Up (on)" : "Down (off)"));
    }

    // ToggleButton keeps state, click/keyboard toggling and ToggleStateChanged; the renderer only paints.
    private static Control IocompToggle(ToggleLook look, string name)
    {
        var t = new ToggleButton { ToggleState = ToggleButtonState.Active, AccessibleName = name, Renderer = new IocompToggleRenderer(look) };
        t.ActiveState.Text = "ON";
        t.ActiveState.BackColor = look == ToggleLook.Led ? Color.FromArgb(0, 127, 0) : Color.Lime;
        t.InactiveState.Text = "OFF";
        t.InactiveState.BackColor = look == ToggleLook.Led ? Color.FromArgb(0, 50, 0) : Color.FromArgb(0, 90, 0);
        return WithState(t, x => x.ToggleState == ToggleButtonState.Active ? "On" : "Off", h => t.ToggleStateChanged += (_, _) => h());
    }

    private static Control WithState<T>(T control, Func<T, string> describe, System.Action<System.Action>? subscribe = null) where T : Control
    {
        var host = new TableLayoutPanel { ColumnCount = 1, RowCount = 2 };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        control.Dock = DockStyle.Fill;
        var readout = new Label { Dock = DockStyle.Fill, ForeColor = Color.DimGray, Text = "State: " + describe(control) };
        void Update() => readout.Text = "State: " + describe(control);
        if (subscribe is not null) subscribe(Update);
        else if (control is CheckBoxAdv box) box.CheckStateChanged += (_, _) => Update();
        host.Controls.Add(control, 0, 0);
        host.Controls.Add(readout, 0, 1);
        return host;
    }

    private static Control Centered(Control c)
    {
        var host = new Panel();
        c.Location = new Point(12, 12);
        host.Controls.Add(c);
        return host;
    }

    private static Control LedButton(bool styled)
    {
        var button = new ButtonAdv { Text = "OFF", Size = new Size(110, 40), Location = new Point(12, 12) };
        var on = false;
        void Apply()
        {
            button.Text = on ? "ON" : "OFF";
            if (styled)
            {
                button.BackColor = on ? Color.LimeGreen : Color.Gray;
                button.ForeColor = Color.White;
            }
        }
        button.Click += (_, _) => { on = !on; Apply(); };
        Apply();
        var host = new Panel();
        host.Controls.Add(button);
        return host;
    }

    private static Control ButtonGrid(int rows, int cols, Func<int, string> text, bool accent = false)
    {
        var grid = new TableLayoutPanel { RowCount = rows, ColumnCount = cols };
        for (var c = 0; c < cols; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
        for (var r = 0; r < rows; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
        for (var i = 0; i < rows * cols; i++)
        {
            var b = new ButtonAdv { Text = text(i), Dock = DockStyle.Fill, Margin = new Padding(2) };
            if (accent) { b.BackColor = Color.SteelBlue; b.ForeColor = Color.White; }
            grid.Controls.Add(b, i % cols, i / cols);
        }
        return grid;
    }

    // Diagram scene. Default: two plain shapes. Restyled (iocomp ObjectCanvas): "Sample Text", a yellow rectangle and
    // an open arc that spins, driven by the shared simulator so the canvas content changes at runtime.
    private static Control DiagramCanvas(bool rotated = false)
    {
        var model = new Model();
        if (!rotated)
        {
            model.AppendChild(new DRectangle(10, 10, 90, 50) { FillStyle = { Color = Color.LightSteelBlue } });
            model.AppendChild(new DRectangle(130, 30, 90, 50) { FillStyle = { Color = Color.Khaki } });
            return new Diagram { Model = model, BackColor = Color.White };
        }

        model.BackgroundStyle.Color = Color.FromArgb(240, 240, 240);
        var text = new TextNode("Sample Text", new RectangleF(8, 6, 170, 34)) { HorizontalAlignment = StringAlignment.Near };
        text.FontStyle.Family = "Segoe UI";
        text.FontStyle.Size = 18;
        text.LineStyle.LineColor = Color.Transparent;
        var box = new DRectangle(120, 45, 45, 95) { FillStyle = { Color = Color.Yellow } };
        box.LineStyle.LineColor = Color.Black;
        var spinner = new Ellipse(30, 95, 34, 34) { FillStyle = { Color = Color.Transparent } };
        spinner.LineStyle.LineColor = Color.FromArgb(50, 50, 50);
        spinner.LineStyle.LineWidth = 1.5f;
        spinner.LineStyle.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        model.AppendChild(text);
        model.AppendChild(box);
        model.AppendChild(spinner);
        var diagram = new Diagram { Model = model, BackColor = Color.FromArgb(240, 240, 240) };
        diagram.View.Grid.Visible = false; // iocomp's canvas has no design grid
        diagram.View.BackgroundColor = Color.FromArgb(240, 240, 240);
        diagram.HScroll = diagram.VScroll = false;
        return Phase2Cards.Live(diagram, s => spinner.RotationAngle = (float)(s.Ramp * 3.6));
    }
}
