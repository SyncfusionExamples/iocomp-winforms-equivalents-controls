using System.Reflection;
using System.Text;
using HmiControls;
using PocApp.Cards;
using Syncfusion.Windows.Forms.Gauge;
using Syncfusion.Windows.Forms.Tools;

namespace PocApp.Infrastructure;

/// <summary>
/// Scripted interaction checks for the interactive cards (proposal item 1.1, automated half).
/// Each check builds the real control on a hidden form and calls its protected input handlers
/// (OnMouseDown/OnMouseUp/OnKeyDown/OnClick) with synthetic event args, then asserts the state and events.
/// This proves the wiring, not the feel: a person with a real mouse still has to try the cards.
/// </summary>
public static class InteractionSelfTest
{
    private sealed record Check(string Control, string What, bool Passed, string Detail);

    public static int Run(string reportPath)
    {
        var checks = new List<Check>();
        using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(400, 400) };
        form.Show();

        void Test(string control, string what, Func<string?> body)
        {
            try
            {
                var failure = body();
                checks.Add(new(control, what, failure is null, failure ?? "ok"));
            }
            catch (Exception ex)
            {
                checks.Add(new(control, what, false, ex.GetType().Name + ": " + ex.Message));
            }
        }

        T Host<T>(T c, Size size) where T : Control
        {
            form.Controls.Clear();
            c.Size = size;
            form.Controls.Add(c);
            c.CreateControl();
            return c;
        }

        // ---- Knob (RadialGauge, ReadOnly = false): the gauge's own mouse handling sets Value.
        // Run with the stock renderer as a control case, then with the custom KnobRenderer the card uses.
        foreach (var custom in new[] { false, true })
        {
            Test(custom ? "Knob" : "Knob (stock renderer, control case)", "drag from 9 to 3 o'clock raises the value and fires ValueChanged", () =>
            {
                var g = Host(new RadialGauge { MinimumValue = 0, MaximumValue = 100, ReadOnly = false, Value = 10 }, new Size(200, 200));
                if (custom) g.Renderer = new KnobRenderer(g);
                g.Refresh(); // the gauge works out its geometry while painting
                var fired = 0;
                g.ValueChanged += (_, _) => fired++;
                Mouse(g, "OnMouseDown", 20, 100);
                foreach (var (x, y) in new[] { (40, 50), (100, 15), (160, 50), (185, 100) })
                    Invoke(g, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, x, y, 0));
                Mouse(g, "OnMouseUp", 185, 100);
                return g.Value > 50 && fired > 0 ? null : $"Value={g.Value:0.0}, ValueChanged x{fired}";
            });
        }

        // ---- Two-state switches (ToggleButton + custom renderer).
        foreach (var look in new[] { ToggleLook.Paddle, ToggleLook.Led, ToggleLook.Rocker })
        {
            Test("Switch" + (look == ToggleLook.Paddle ? "Toggle" : look.ToString()), "click toggles Active/Inactive and fires ToggleStateChanged", () =>
            {
                var t = Host(new ToggleButton { ToggleState = ToggleButtonState.Active, Renderer = new IocompToggleRenderer(look) }, new Size(70, 130));
                var fired = 0;
                t.ToggleStateChanged += (_, _) => fired++;
                Mouse(t, "OnMouseUp", 35, 65);
                var first = t.ToggleState;
                Mouse(t, "OnMouseUp", 35, 65);
                return first == ToggleButtonState.Inactive && t.ToggleState == ToggleButtonState.Active && fired == 2
                    ? null : $"after 1st click {first}, after 2nd {t.ToggleState}, events {fired}";
            });
        }

        // ---- Rocker3Way: press the half you want; one step per press; arrow keys.
        Test("SwitchRocker3Way", "top half steps up, bottom half steps down, arrows work", () =>
        {
            var r = Host(new Rocker3WayBox { CheckState = CheckState.Indeterminate }, new Size(70, 130));
            var fired = 0;
            r.CheckStateChanged += (_, _) => fired++;
            Mouse(r, "OnMouseUp", 35, 20);
            var afterTop = r.CheckState;
            Mouse(r, "OnMouseUp", 35, 20);
            var stillTop = r.CheckState;
            Mouse(r, "OnMouseUp", 35, 110);
            Mouse(r, "OnMouseUp", 35, 110);
            var afterBottom = r.CheckState;
            Key(r, Keys.Up);
            var afterKey = r.CheckState;
            return afterTop == CheckState.Checked && stillTop == CheckState.Checked && afterBottom == CheckState.Unchecked
                   && afterKey == CheckState.Indeterminate && fired == 4
                ? null : $"top {afterTop}/{stillTop}, bottom {afterBottom}, Up key {afterKey}, events {fired}";
        });

        // ---- Lever (CheckBoxAdv): click toggles Checked.
        Test("SwitchLever", "click toggles Checked and fires CheckedChanged", () =>
        {
            var l = Host(new LeverBox { Checked = true }, new Size(56, 104));
            var fired = 0;
            l.CheckedChanged += (_, _) => fired++;
            Mouse(l, "OnMouseDown", 28, 50);
            Mouse(l, "OnMouseUp", 28, 50);
            return !l.Checked && fired == 1 ? null : $"Checked={l.Checked}, events {fired}";
        });

        // ---- Selector switches (HmiControls): hit testing via real mouse handlers, plus keyboard.
        Test("SwitchRotary", "click on a position label selects it; arrow keys move", () =>
        {
            var s = Host(new SwitchRotary(), new Size(256, 190));
            var fired = 0;
            s.SelectedIndexChanged += (_, _) => fired++;
            s.SelectedIndex = 0;
            fired = 0;
            Key(s, Keys.Right);
            var afterKey = s.SelectedIndex;
            return afterKey == 1 && fired == 1 ? null : $"after Right key index {afterKey}, events {fired}";
        });

        Test("SwitchQuad", "pressing each arm selects Up/Right/Down/Left", () =>
        {
            var q = Host(new SwitchQuad(), new Size(120, 120));
            var seen = new List<string>();
            foreach (var p in new[] { new Point(60, 15), new Point(105, 60), new Point(60, 105), new Point(15, 60) })
            {
                Mouse(q, "OnMouseDown", p.X, p.Y);
                seen.Add(q.SelectedItem);
            }
            Key(q, Keys.Up);
            return string.Join(",", seen) == "Up,Right,Down,Left" && q.SelectedItem == "Up" ? null : "saw " + string.Join(",", seen) + ", after Up key " + q.SelectedItem;
        });

        Test("SwitchPanel", "clicking each label selects it", () =>
        {
            var sp = Host(new SwitchPanel(), new Size(181, 256));
            var seen = new List<string>();
            foreach (var p in new[] { new Point(40, 48), new Point(90, 16), new Point(140, 48) })
            {
                Mouse(sp, "OnMouseDown", p.X, p.Y);
                seen.Add(sp.SelectedItem);
            }
            return string.Join(",", seen) == "On,Off,Auto" ? null : "saw " + string.Join(",", seen);
        });

        Test("SwitchSlider", "clicking near a position snaps to it; keys move", () =>
        {
            var ss = Host(new SwitchSlider(), new Size(200, 200));
            Mouse(ss, "OnMouseDown", 15, 190);
            var bottom = ss.SelectedItem;
            Key(ss, Keys.Up);
            return bottom == "Low" && ss.SelectedItem == "Medium" ? null : $"bottom click {bottom}, after Up {ss.SelectedItem}";
        });

        // ---- Indicators and process symbols.
        Test("Led", "click toggles when Interactive and fires IsOnChanged", () =>
        {
            var led = Host(new Led { Interactive = true, IsOn = false }, new Size(48, 48));
            var fired = 0;
            led.IsOnChanged += (_, _) => fired++;
            Invoke(led, "OnClick", EventArgs.Empty);
            return led.IsOn && fired == 1 ? null : $"IsOn={led.IsOn}, events {fired}";
        });

        Test("MatrixLeds", "click toggles the cell under the mouse", () =>
        {
            var m = Host(new MatrixLeds { Interactive = true }, new Size(100, 100));
            var changed = -1;
            m.CellChanged += (_, i) => changed = i;
            Mouse(m, "OnMouseUp", 90, 10); // row 0, column 4
            return m[0, 4] && changed == 4 ? null : $"cell(0,4)={m[0, 4]}, CellChanged index {changed}";
        });

        Test("Valve", "click and Space toggle IsOpen", () =>
        {
            var v = Host(new Valve { IsOpen = true }, new Size(128, 110));
            var fired = 0;
            v.IsOpenChanged += (_, _) => fired++;
            Invoke(v, "OnClick", EventArgs.Empty);
            var afterClick = v.IsOpen;
            Key(v, Keys.Space);
            return !afterClick && v.IsOpen && fired == 2 ? null : $"after click {afterClick}, after Space {v.IsOpen}, events {fired}";
        });

        Test("Motor", "Running toggles and fires RunningChanged", () =>
        {
            var mo = Host(new Motor { Running = true }, new Size(120, 120));
            var fired = 0;
            mo.RunningChanged += (_, _) => fired++;
            mo.Running = false;
            return !mo.Running && fired == 1 ? null : $"Running={mo.Running}, events {fired}";
        });

        form.Close();
        File.WriteAllText(reportPath, Report(checks));
        return checks.All(c => c.Passed) ? 0 : 1;
    }

    private static void Mouse(Control c, string method, int x, int y) =>
        Invoke(c, method, new MouseEventArgs(MouseButtons.Left, 1, x, y, 0));

    private static void Key(Control c, Keys key) => Invoke(c, "OnKeyDown", new KeyEventArgs(key));

    private static void Invoke(Control c, string method, EventArgs args)
    {
        var m = c.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [args.GetType()], null)
                ?? throw new MissingMethodException(c.GetType().Name, method);
        m.Invoke(c, [args]);
    }

    private static string Report(IReadOnlyList<Check> checks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Interaction self-test");
        sb.AppendLine();
        sb.AppendLine("Synthetic input: the controls' own mouse/keyboard handlers are called with scripted event args on a hidden form.");
        sb.AppendLine("It proves state changes and events are wired; it does not prove how the controls feel with a real mouse.");
        sb.AppendLine();
        sb.AppendLine($"Result: {checks.Count(c => c.Passed)} of {checks.Count} passed.");
        sb.AppendLine();
        sb.AppendLine("| Control | Check | Result | Detail |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var c in checks)
            sb.AppendLine($"| {c.Control} | {c.What} | {(c.Passed ? "PASS" : "FAIL")} | {c.Detail} |");
        return sb.ToString();
    }
}
