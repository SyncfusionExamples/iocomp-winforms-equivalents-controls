using HmiControls;
using PocApp.Infrastructure;
using Syncfusion.Windows.Forms.Gauge;
using FlowDirection = HmiControls.FlowDirection;

namespace PocApp.Cards;

/// <summary>Cards for the five built HmiControls prototypes: Led, Odometer, SwitchRotary, Pipe, Valve.</summary>
public static class Phase4Cards
{
    public static void Register()
    {
        Phase2Cards.Add("Led", () => LedPanel());
        Phase2Cards.Add("Odometer", () => OdometerPanel());
        Phase2Cards.Add("SwitchRotary", () => RotaryPanel());
        Phase2Cards.Add("Pipe", () => PipeValveScene());
        Phase2Cards.Add("Valve", () => PipeValveScene());
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
