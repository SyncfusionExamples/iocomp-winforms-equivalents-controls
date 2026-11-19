using HmiControls;
using PocApp.Infrastructure;
using Syncfusion.Windows.Forms.Gauge;
using FlowDirection = HmiControls.FlowDirection;

namespace PocApp.Cards;

/// <summary>Cards for the built HmiControls prototypes (first five, then the eight built in the second pass).</summary>
public static class Phase4Cards
{
    public static void Register()
    {
        Phase2Cards.Add("Led", () => LedPanel());
        Phase2Cards.Add("Odometer", () => OdometerPanel());
        Phase2Cards.Add("SwitchRotary", () => RotaryPanel());
        Phase2Cards.Add("Pipe", () => PipeValveScene());
        Phase2Cards.Add("Valve", () => PipeValveScene());

        // Second batch of prototypes (former placeholders), plus SwitchSlider which replaces the TrackBarEx stand-in.
        Phase2Cards.Add("LedArrow", () =>
        {
            var a = new LedArrow { Width = 128, Height = 64, AccessibleName = "Arrow indicator driven by the step signal" };
            return Phase2Cards.Live(Fixed(a), s => a.IsOn = s.Step);
        });
        Phase2Cards.Add("MatrixLeds", () =>
        {
            var m = new MatrixLeds { Size = new Size(150, 150), Interactive = true, AccessibleName = "LED matrix, click a cell to toggle" };
            // A diagonal band lights up in step with the ramp so the grid visibly changes.
            return Phase2Cards.Live(Fixed(m), s =>
            {
                var k = (int)(s.Ramp / 20);
                for (var r = 0; r < m.Rows; r++)
                    for (var c = 0; c < m.Columns; c++)
                        m[r, c] = (r + c) % 5 == k;
            });
        });
        Phase2Cards.Add("SwitchQuad", () => WithReadout(new SwitchQuad { Size = new Size(140, 140), AccessibleName = "Four-way switch" }));
        Phase2Cards.Add("SwitchPanel", () => WithReadout(new SwitchPanel { Size = new Size(120, 170), AccessibleName = "Panel switch" }));
        Phase2Cards.Add("SwitchSlider", () => WithReadout(new SwitchSlider { Size = new Size(170, 160), AccessibleName = "Slider switch" }));
        Phase2Cards.Add("RotationDisplay", () =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "iocomp-dotnet-images", "Label.png");
            var r = new RotationDisplay { Size = new Size(160, 160), Text = "Rotated text", Font = new Font("Segoe UI", 14f), AccessibleName = "Rotation display" };
            if (File.Exists(path)) r.Image = Image.FromFile(path);
            return Phase2Cards.Live(Fixed(r), s => r.Angle = (float)(s.Ramp * 3.6));
        });
        Phase2Cards.Add("PipeJoint", () => JointScene());
        Phase2Cards.Add("Motor", () =>
        {
            var m = new Motor { Size = new Size(160, 160), AccessibleName = "Motor, runs while the step signal is high" };
            return Phase2Cards.Live(Fixed(m), s => m.Running = s.Step);
        });
    }

    private static Control Fixed(Control c)
    {
        var host = new Panel();
        c.Location = new Point(8, 4);
        host.Controls.Add(c);
        return host;
    }

    private static Control WithReadout(SelectorSwitch sw)
    {
        var readout = new Label { AutoSize = true, ForeColor = Color.DimGray, Text = "Selected: " + sw.SelectedItem };
        sw.SelectedIndexChanged += (_, _) => readout.Text = "Selected: " + sw.SelectedItem;
        sw.Location = new Point(8, 4);
        readout.Location = new Point(sw.Right + 12, 8);
        var host = new Panel();
        host.Controls.AddRange([sw, readout]);
        return host;
    }

    // Two pipe runs joined by a PipeJoint, as the joint would be used.
    private static Control JointScene()
    {
        var host = new Panel();
        var joint = new PipeJoint { Location = new Point(110, 4), Size = new Size(40, 150) };
        var left = new Pipe { Location = new Point(10, 30), Size = new Size(100, 26) };
        var right = new Pipe { Location = new Point(150, 100), Size = new Size(100, 26) };
        host.Controls.AddRange([left, joint, right]);
        return host;
    }

    private static Control LedPanel()
    {
        var host = new FlowLayoutPanel { WrapContents = true };
        var green = new Led { OnColor = Color.Lime, Interactive = true, AccessibleName = "Green LED, click to toggle" };
        var red = new Led { OnColor = Color.Red, IsOn = true, Interactive = true, AccessibleName = "Red LED, click to toggle" };
        var blink = new Led { OnColor = Color.Orange, AccessibleName = "Orange LED driven by the step signal" };
        host.Controls.AddRange([green, red, blink, new Label { Text = "Click an LED to toggle. Orange follows the step signal.", AutoSize = true, Margin = new Padding(8, 16, 0, 0) }]);
        return Phase2Cards.Live(host, s => blink.IsOn = s.Step);
    }

    private static Control OdometerPanel()
    {
        var host = new TableLayoutPanel { ColumnCount = 1, RowCount = 3 };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // default AutoSize column was wider than the slot and clipped the digits
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var odo = new Odometer { Dock = DockStyle.Fill, DigitCount = 6, ShowLeadingZeros = true, AccessibleName = "Odometer" };
        var standIn = new DigitalGauge { Dock = DockStyle.Fill, CharacterCount = 6, CharacterType = CharacterType.SevenSegment, Value = "000000", MinimumSize = new Size(0, 0) };

        // The odometer follows the ramp; the DigitalGauge underneath is the closest Syncfusion stand-in for comparison.
        host.Controls.Add(odo, 0, 0);
        host.Controls.Add(standIn, 0, 1);
        host.Controls.Add(new Label { Text = "Top: HmiControls.Odometer (rolling). Bottom: DigitalGauge stand-in (jumps).", Dock = DockStyle.Fill, ForeColor = Color.DimGray }, 0, 2);
        return Phase2Cards.Live(host, s =>
        {
            var v = Math.Floor(s.Ramp * 12);
            odo.Value = v;
            standIn.Value = ((long)v).ToString("D6");
        });
    }

    private static Control RotaryPanel()
    {
        var host = new TableLayoutPanel { ColumnCount = 1, RowCount = 2 };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        var rotary = new SwitchRotary { Dock = DockStyle.Fill, AccessibleName = "Rotary switch" };
        var readout = new Label { Dock = DockStyle.Fill, Text = "Selected: Medium", ForeColor = Color.DimGray };
        rotary.SelectedIndexChanged += (_, _) => readout.Text = "Selected: " + rotary.SelectedItem;
        host.Controls.Add(rotary, 0, 0);
        host.Controls.Add(readout, 0, 1);
        return host;
    }

    // One scene for both Pipe and Valve: inlet pipe -> valve -> outlet pipe. Closing the valve stops the outlet flow.
    private static Control PipeValveScene()
    {
        var host = new FlowLayoutPanel { WrapContents = false, FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight };
        var inlet = new Pipe { Width = 110, Height = 28, Margin = new Padding(0, 50, 0, 0) };
        var valve = new Valve { Width = 100, Height = 90, AccessibleName = "Valve, press Space to toggle" };
        var outlet = new Pipe { Width = 110, Height = 28, Margin = new Padding(0, 50, 0, 0), Direction = FlowDirection.Forward };
        valve.IsOpenChanged += (_, _) => outlet.Flowing = valve.IsOpen;
        host.Controls.AddRange([inlet, valve, outlet]);
        return host;
    }
}
