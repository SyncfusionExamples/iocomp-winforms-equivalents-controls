using PocApp.Infrastructure;
using Syncfusion.Windows.Forms;
using Syncfusion.Windows.Forms.Gauge;
using Syncfusion.Windows.Forms.Tools;
using GRange = Syncfusion.Windows.Forms.Gauge.Range;
using GThemeStyle = Syncfusion.Windows.Forms.Gauge.ThemeStyle;

namespace PocApp.Cards;

/// <summary>Live Syncfusion controls for the Gauges, Digital, Clocks and Inputs categories.</summary>
public static class Phase2Cards
{
    private static readonly Color Green = Color.FromArgb(46, 160, 67);
    private static readonly Color Amber = Color.FromArgb(240, 180, 0);
    private static readonly Color Red = Color.FromArgb(210, 50, 50);

    public static void Register()
    {
        RegisterGauges();
        RegisterDigital();
        RegisterClocks();
        RegisterInputs();
    }

    // Calls update on every simulator tick and detaches when the control is disposed.
    internal static T Live<T>(T control, Action<SignalSimulator> update) where T : Control
    {
        void Handler(object? s, EventArgs e) => update(SignalSimulator.Shared);
        SignalSimulator.Shared.Tick += Handler;
        control.Disposed += (_, _) => SignalSimulator.Shared.Tick -= Handler;
        return control;
    }

    internal static void Add(string name, Func<Control> defaultView, Func<Control>? restyled = null)
    {
        CardFactories.Default[name] = defaultView;
        if (restyled is not null) CardFactories.Restyled[name] = restyled;
    }

    // ---------------------------------------------------------------- Gauges

    private static RadialGauge Radial(float min = 0, float max = 100, float major = 20)
    {
        var g = new RadialGauge { MinimumValue = min, MaximumValue = max, MajorDifference = major, ReadOnly = true };
        g.Ranges.Add(new GRange { StartValue = min, EndValue = min + (max - min) * 0.6f, Color = Green, Height = 8 });
        g.Ranges.Add(new GRange { StartValue = min + (max - min) * 0.6f, EndValue = min + (max - min) * 0.85f, Color = Amber, Height = 8 });
        g.Ranges.Add(new GRange { StartValue = min + (max - min) * 0.85f, EndValue = max, Color = Red, Height = 8 });
        return g;
    }

    private static LinearGauge Linear(bool vertical = true)
    {
        var g = new LinearGauge
        {
            MinimumValue = 0,
            MaximumValue = 100,
            MajorDifference = 20,
            ReadOnly = true,
            LinearFrameType = vertical ? LinearFrameType.Vertical : LinearFrameType.Horizontal,
        };
        g.Ranges.Add(new LinearRange { StartValue = 0, EndValue = 60, Color = Green, Height = 8, StartWidth = 8, EndWidth = 8 });
        g.Ranges.Add(new LinearRange { StartValue = 60, EndValue = 85, Color = Amber, Height = 8, StartWidth = 8, EndWidth = 8 });
        g.Ranges.Add(new LinearRange { StartValue = 85, EndValue = 100, Color = Red, Height = 8, StartWidth = 8, EndWidth = 8 });
        return g;
    }

    private static void DarkRadial(RadialGauge g, Color accent)
    {
        g.BackgroundGradientStartColor = Color.FromArgb(40, 44, 52);
        g.BackgroundGradientEndColor = Color.FromArgb(18, 20, 24);
        g.OuterFrameGradientStartColor = Color.FromArgb(90, 90, 96);
        g.OuterFrameGradientEndColor = Color.FromArgb(30, 30, 34);
        g.InnerFrameGradientStartColor = Color.FromArgb(60, 60, 66);
        g.InnerFrameGradientEndColor = Color.FromArgb(20, 20, 24);
        g.ScaleLabelColor = Color.Gainsboro;
        g.MajorTickMarkColor = Color.Gainsboro;
        g.MinorTickMarkColor = Color.Gray;
        g.NeedleColor = accent;
        g.GaugeValueColor = accent;
    }

    private static readonly Color IoGreen = Color.FromArgb(50, 205, 50);
    private static readonly Color IoYellow = Color.FromArgb(230, 230, 60);
    private static readonly Color IoRed = Color.FromArgb(230, 30, 30);
    private static readonly Color IoBack = Color.FromArgb(240, 240, 240);

    // iocomp scale labels use one decimal ("0.0", "20.0"); RadialGauge exposes this through the DrawLabel event.
    private static void OneDecimalLabels(RadialGauge g) => g.DrawLabel += (_, e) =>
    {
        if (e.LabelType != LabelType.Scale || !float.TryParse(e.Text, out var v)) return;
        e.Text = v.ToString("0.0");
        e.Font = g.Font;
        e.ForeColor = g.ScaleLabelColor;
        e.Handled = true;
    };

    private static void FlatBackground(RadialGauge g)
    {
        g.BackgroundGradientStartColor = g.BackgroundGradientEndColor = IoBack;
        g.OuterFrameGradientStartColor = g.OuterFrameGradientEndColor = IoBack;
        g.InnerFrameGradientStartColor = g.InnerFrameGradientEndColor = IoBack;
        g.BackColor = IoBack;
    }

    private static RadialGauge Rendered(RadialGauge g, Func<RadialGauge, IRadialGaugeRenderer> renderer)
    {
        g.BackColor = IoBack;
        g.Renderer = renderer(g);
        return g;
    }

    private static LinearGauge Rendered(LinearGauge g, Func<LinearGauge, ILinearGaugeRenderer> renderer)
    {
        g.BackColor = IoBack;
        g.Renderer = renderer(g);
        return g;
    }

    private static void RegisterGauges()
    {
        // A: properties only. Quarter arc, thick green/yellow/red band with black ticks on it, "0.0" labels, black needle.
        Add("GaugeAngular", () =>
        {
            var g = new RadialGauge
            {
                MinimumValue = 0, MaximumValue = 100, MajorDifference = 10, ReadOnly = true,
                FrameType = FrameType.QuarterCircle, StartAngle = 180,
                NeedleStyle = NeedleStyle.Advanced, NeedleColor = Color.Black,
                GaugeArcColor = IoBack, ArcThickness = 1,
                MajorTickMarkColor = Color.Black, MinorTickMarkColor = Color.Black, InterLinesColor = Color.Black,
                MajorTickMarkHeight = 12, MinorTickMarkHeight = 5, MinorInnerLinesHeight = 5,
                ScaleLabelColor = Color.Black, ShowGaugeValue = false, GaugeLabel = "",
                LabelPlacement = Syncfusion.Windows.Forms.Gauge.LabelPlacement.Outside, TickPlacement = Syncfusion.Windows.Forms.Gauge.TickPlacement.OutSide,
                Font = new Font("Segoe UI", 7.5f),
            };
            FlatBackground(g);
            g.Ranges.Add(new GRange { StartValue = 0, EndValue = 50, Color = IoGreen, Height = 12, RangePlacement = Syncfusion.Windows.Forms.Gauge.TickPlacement.OutSide });
            g.Ranges.Add(new GRange { StartValue = 50, EndValue = 75, Color = IoYellow, Height = 12, RangePlacement = Syncfusion.Windows.Forms.Gauge.TickPlacement.OutSide });
            g.Ranges.Add(new GRange { StartValue = 75, EndValue = 100, Color = IoRed, Height = 12, RangePlacement = Syncfusion.Windows.Forms.Gauge.TickPlacement.OutSide });
            OneDecimalLabels(g);
            return Live(g, s => g.Value = (float)s.Sine);
        });

        // A: properties only. Flat background, green/yellow/red band beside the ticks, blue pointer.
        Add("GaugeLinear", () =>
        {
            var g = new LinearGauge
            {
                MinimumValue = 0, MaximumValue = 100, MajorDifference = 20, MinorTickCount = 3, ReadOnly = true,
                LinearFrameType = LinearFrameType.Vertical,
                BackColor = IoBack,
                BackgroundGradientStartColor = IoBack, BackgroundGradientEndColor = IoBack,
                OuterFrameGradientStartColor = IoBack, OuterFrameGradientEndColor = IoBack,
                InnerFrameGradientStartColor = IoBack, InnerFrameGradientEndColor = IoBack,
                MajorTickMarkColor = Color.Black, MinorTickMarkColor = Color.Black, ScaleLabelColor = Color.Black,
                NeedleColor = Color.Blue, ValueIndicatorColor = Color.Blue, ScaleColor = Color.Black,
            };
            g.Ranges.Add(new LinearRange { StartValue = 0, EndValue = 50, Color = IoGreen, Height = 8, StartWidth = 8, EndWidth = 8 });
            g.Ranges.Add(new LinearRange { StartValue = 50, EndValue = 75, Color = IoYellow, Height = 8, StartWidth = 8, EndWidth = 8 });
            g.Ranges.Add(new LinearRange { StartValue = 75, EndValue = 100, Color = IoRed, Height = 8, StartWidth = 8, EndWidth = 8 });
            return Live(g, s => g.Value = (float)s.Sine);
        });

        // E: restyled views below paint through a custom renderer (GaugeRenderers.cs); the gauge keeps value, range and colours.
        Add("GaugeTube", () =>
        {
            var g = Linear();
            return Live(g, s => g.Value = (float)s.Sine);
        }, () =>
        {
            var g = Rendered(new LinearGauge
            {
                MinimumValue = 0, MaximumValue = 100, MajorDifference = 20, MinorTickCount = 3, ReadOnly = true,
                ValueIndicatorColor = Color.FromArgb(70, 70, 255),
                MajorTickMarkColor = Color.Black, MinorTickMarkColor = Color.Black, ScaleLabelColor = Color.Black,
            }, lg => new TubeRenderer(lg, bulb: false));
            return Live(g, s => g.Value = (float)s.Sine);
        });

        Add("Thermometer", () =>
        {
            var g = Linear();
            return Live(g, s => g.Value = (float)s.Sine);
        }, () =>
        {
            var g = Rendered(new LinearGauge
            {
                MinimumValue = 0, MaximumValue = 100, MajorDifference = 20, MinorTickCount = 3, ReadOnly = true,
                ValueIndicatorColor = Color.FromArgb(225, 35, 35),
                MajorTickMarkColor = Color.Black, MinorTickMarkColor = Color.Black, ScaleLabelColor = Color.Black,
            }, lg => new TubeRenderer(lg, bulb: true));
            return Live(g, s => g.Value = (float)s.Sine);
        });

        // Knob: ReadOnly=false lets the user drag the pointer. Behaviour is checked by hand (see GAP_ANALYSIS).
        Add("Knob", () =>
        {
            var g = new RadialGauge { MinimumValue = 0, MaximumValue = 100, MajorDifference = 20, ReadOnly = false, Value = 30 };
            return g;
        }, () => Rendered(new RadialGauge
        {
            MinimumValue = 0, MaximumValue = 100, MajorDifference = 10, ReadOnly = false, Value = 85,
            StartAngle = 135, SweepAngle = 270,
            NeedleColor = Color.Red, MajorTickMarkColor = Color.Black, MinorTickMarkColor = Color.Black, ScaleLabelColor = Color.Black,
        }, rg => new KnobRenderer(rg)));

        Add("Compass", () =>
        {
            var g = new RadialGauge { MinimumValue = 0, MaximumValue = 360, MajorDifference = 45, ReadOnly = true, StartAngle = 270, SweepAngle = 360, GaugeLabel = "Heading" };
            return Live(g, s => g.Value = (float)(s.Ramp * 3.6));
        }, () =>
        {
            var g = Rendered(new RadialGauge
            {
                MinimumValue = 0, MaximumValue = 360, MajorDifference = 45, ReadOnly = true, StartAngle = 270, SweepAngle = 360,
                BackgroundGradientStartColor = Color.FromArgb(0, 0, 139),
                MajorTickMarkColor = Color.Cyan, ScaleLabelColor = Color.White,
                NeedleColor = Color.Yellow, GaugeValueColor = Color.Black,
            }, rg => new CompassRenderer(rg));
            return Live(g, s => g.Value = (float)(s.Ramp * 3.6));
        });

        Add("LedBar", () =>
        {
            var g = Linear(vertical: false);
            return Live(g, s => g.Value = (float)s.Sine);
        }, () =>
        {
            var g = Rendered(new LinearGauge
            {
                MinimumValue = 0, MaximumValue = 100, ReadOnly = true,
                BackgroundGradientStartColor = Color.Black,
            }, lg => new LedBarRenderer(lg));
            g.Ranges.Add(new LinearRange { StartValue = 0, EndValue = 60, Color = Color.Lime });
            g.Ranges.Add(new LinearRange { StartValue = 60, EndValue = 75, Color = Color.Yellow });
            g.Ranges.Add(new LinearRange { StartValue = 75, EndValue = 100, Color = Color.Red });
            return Live(g, s => g.Value = (float)s.Sine);
        });

        Add("LedSpiral", () =>
        {
            var g = Radial();
            return Live(g, s => g.Value = (float)s.Sine);
        }, () =>
        {
            // Full ring starting at 9 o'clock, clockwise, as in the iocomp image.
            var g = Rendered(new RadialGauge { MinimumValue = 0, MaximumValue = 100, ReadOnly = true, StartAngle = 180, SweepAngle = 360 },
                rg => new LedSpiralRenderer(rg));
            g.Ranges.Add(new GRange { StartValue = 0, EndValue = 50, Color = Color.Lime });
            g.Ranges.Add(new GRange { StartValue = 50, EndValue = 75, Color = Color.Yellow });
            g.Ranges.Add(new GRange { StartValue = 75, EndValue = 100, Color = Color.FromArgb(255, 0, 0) });
            return Live(g, s => g.Value = (float)s.Sine);
        });

        // Tank (N): LinearGauge keeps the value/range; a custom ILinearGaugeRenderer draws the vessel and liquid.
        Add("Tank", () =>
        {
            var g = new LinearGauge
            {
                MinimumValue = 0, MaximumValue = 100, MajorDifference = 25, ReadOnly = true,
                LinearFrameType = LinearFrameType.Vertical, ShowNeedle = false,
                ValueIndicatorColor = Color.Blue,
            };
            return Live(Rendered(g, x => new TankRenderer(x)), s => g.Value = (float)s.Sine);
        });

        // SlidingScale / SlidingCompass (N): LinearGauge + custom renderer that scrolls the tape under a fixed centre line.
        Add("SlidingScale", () =>
        {
            var g = SlidingTape(800, 1000);
            return Live(Rendered(g, x => new SlidingTapeRenderer(x, span: 120, wrap: false)), s => g.Value = 800 + (float)s.Sine * 2);
        });
        Add("SlidingCompass", () =>
        {
            var g = SlidingTape(0, 360);
            return Live(Rendered(g, x => new SlidingTapeRenderer(x, span: 120, wrap: true)), s => g.Value = (float)(s.Ramp * 3.6));
        });
    }

    private static LinearGauge SlidingTape(float min, float max) => new()
    {
        MinimumValue = min, MaximumValue = max, MajorDifference = 20, MinorTickCount = 1, ReadOnly = true,
        LinearFrameType = LinearFrameType.Vertical, ShowNeedle = false,
        MajorTickMarkColor = Color.Black, ScaleLabelColor = Color.Black,
        ValueIndicatorColor = Color.Blue, NeedleColor = Color.Yellow,
    };

    // ---------------------------------------------------------------- Digital

    private static DigitalGauge Digit(Func<SignalSimulator, string> text, int chars, CharacterType type = CharacterType.SevenSegment, bool styled = false, Color? lit = null)
    {
        var g = new DigitalGauge
        {
            CharacterCount = chars,
            CharacterType = type,
            ShowInvisibleSegments = true,
            MinimumSize = new Size(90, 60),
        };
        if (styled) IocompDigital(g, type, lit);
        return Live(g, s => g.Value = text(s));
    }

    // iocomp seven-segment look, sampled from the reference images: black face, lit 00FF00, unlit segments at half
    // brightness (007F00), square corners, thin light-grey sunken frame. LcdMatrix: teal face, cyan dots.
    private static void IocompDigital(DigitalGauge g, CharacterType type, Color? lit)
    {
        var matrix = type == CharacterType.DotMatrixSegment;
        var on = lit ?? (matrix ? Color.Cyan : Color.Lime);
        var face = matrix ? Color.FromArgb(0, 128, 128) : Color.Black;
        g.RoundCornerRadius = 0;
        g.ThemeBrush.Add(new DigitalGaugeThemeBrush
        {
            BackgroundGradientStartColor = face,
            BackgroundGradientEndColor = face,
            OuterFrameGradientStartColor = Color.FromArgb(247, 247, 247),
            OuterFrameGradientEndColor = Color.FromArgb(204, 204, 204),
            FrameBorderColor = Color.FromArgb(120, 120, 120),
            ForeColor = on,
            InactiveTextColor = matrix ? Color.FromArgb(95, 158, 160) : GaugeDraw.Dim(on),
        });
        g.VisualStyle = GThemeStyle.Custom;
    }

    private static void RegisterDigital()
    {
        // A: still properties only, so the iocomp palette is applied to the single (default) view. iocomp shows the sign.
        Add("SevenSegmentInteger", () => Digit(s => "+" + ((int)(s.Sine * 500)).ToString("00000"), 6, styled: true));

        Add("SevenSegmentAnalog",
            () => Digit(s => s.Sine.ToString("0.00"), 6),
            () => Digit(s => "+" + (s.Sine * 4).ToString("000.00"), 7, styled: true));

        Add("SevenSegmentBinary",
            () => Digit(s => Convert.ToString((int)s.Ramp, 2).PadLeft(8, '0'), 8),
            () => Digit(s => Convert.ToString((int)(s.Ramp * 0.63), 2).PadLeft(6, '0'), 6, styled: true));

        Add("SevenSegmentHexadecimal",
            () => Digit(s => ((int)(s.Ramp * 655)).ToString("X4"), 4),
            () => Digit(s => ((int)(s.Ramp * 655)).ToString("X6"), 6, styled: true));

        Add("SevenSegmentCharacter",
            () => Digit(s => s.Step ? "HELLO" : "AbCdE", 5),
            () => Digit(s => s.Step ? "8" : "A", 1, styled: true));

        Add("LcdMatrix",
            () => Digit(s => s.Step ? "TEMP 25" : "PRESS 3", 8, CharacterType.DotMatrixSegment),
            () => Digit(s => s.Step ? "LCDMATRIX1" : "TEMP 25.0", 10, CharacterType.DotMatrixSegment, styled: true));

        // Odometer (N): DigitalGauge has no rolling-digit animation, so a counter is the stand-in.
        Add("Odometer", () =>
        {
            var count = 0;
            return Digit(s => (count++ / 4).ToString("000000"), 6);
        });
    }

    // ---------------------------------------------------------------- Clocks

    private static void RegisterClocks()
    {
        Add("ClockAnalog",
            () => FixedSize(new Clock { ClockType = ClockTypes.Analog }),
            () => FixedSize(IocompClock()));

        Add("SevenSegmentClock",
            () => Digit(s => s.Now.ToString("HH:mm:ss"), 8),
            () => Digit(s => s.Now.ToString("HH:mm:ss"), 8, styled: true));

        Add("SevenSegmentClockSmpte",
            () => Digit(s => $"{s.Now:HH:mm:ss}:{s.Now.Millisecond * 25 / 1000:00}", 11),
            () => Digit(s => $"{s.Now:HH:mm:ss}:{s.Now.Millisecond * 25 / 1000:00}", 11, styled: true));
    }

    // iocomp ClockAnalog: round grey face with a light rim, white numerals, tapered silver hands, thin white second hand.
    private static Clock IocompClock() => new()
    {
        ClockType = ClockTypes.Analog,
        VisualStyle = ClockVisualStyle.None,
        ClockShape = ClockShapes.Circle,
        ClockFrame = ClockFrames.CircularFrame,
        ShowClockFrame = true,
        ShowBorder = false,
        StartGradientBackColor = Color.FromArgb(150, 150, 150),
        EndGradientBackColor = Color.FromArgb(125, 125, 125),
        BorderColor = Color.FromArgb(214, 214, 214),
        ForeColor = Color.White,
        MinuteColor = Color.White,
        HourHandColor = Color.FromArgb(225, 225, 225),
        MinuteHandColor = Color.FromArgb(225, 225, 225),
        SecondHandColor = Color.White,
        ShowSecondHand = true,
        Renderer = new TaperedHandsClockRenderer(),
    };

    // The Clock control did not scale to its parent in the first run (it was cropped), so give it a fixed size.
    private static Control FixedSize(Clock clock)
    {
        clock.Size = new Size(170, 170);
        if (clock.Renderer is TaperedHandsClockRenderer)
        {
            // Clip the round iocomp face so the Clock's square drop shadow is not painted.
            using var round = new System.Drawing.Drawing2D.GraphicsPath();
            round.AddEllipse(0, 0, clock.Width - 6, clock.Height - 6);
            clock.Region = new Region(round);
        }
        if (SignalSimulator.Shared.IsFrozen)
        {
            // Snapshot mode: show the fixed time so regression images are repeatable.
            clock.ShowCustomTimeClock = true;
            clock.CustomTime = SignalSimulator.Shared.Now;
            clock.StopTimer = true;
        }
        clock.Location = new Point(4, 4);
        var host = new Panel();
        host.Controls.Add(clock);
        return host;
    }

    // ---------------------------------------------------------------- Inputs

    // iocomp Display* look (from the reference images): cyan text on black, thin grey frame. Properties only.
    private static TextBoxExt IocompDisplay() => new()
    {
        ReadOnly = true,
        TextAlign = HorizontalAlignment.Center,
        Font = new Font("Segoe UI", 16f),
        BackColor = Color.Black,
        ForeColor = Color.Cyan,
        BorderStyle = BorderStyle.FixedSingle,
        BorderColor = Color.FromArgb(160, 160, 160),
    };

    private static void RegisterInputs()
    {
        Add("DisplayDouble", () =>
        {
            var t = IocompDisplay();
            return Live(t, s =>
            {
                t.Text = (s.Sine * 100).ToString("0.00");
                t.ForeColor = s.Sine > 85 ? Color.Red : Color.Cyan; // alarm colour by code
            });
        });

        Add("DisplayInteger", () =>
        {
            var t = IocompDisplay();
            return Live(t, s =>
            {
                t.Text = ((int)(s.Sine * 100)).ToString();
                t.ForeColor = s.Sine > 85 ? Color.Red : Color.Cyan;
            });
        });

        Add("DisplayString", () =>
        {
            var t = IocompDisplay();
            return Live(t, s => t.Text = s.Step ? "RUNNING" : "IDLE");
        });

        Add("EditDouble", () => new DoubleTextBox { MinValue = 0, MaxValue = 100, DoubleValue = 25.5, Font = new Font("Segoe UI", 14f) });
        Add("EditInteger", () => new IntegerTextBox { MinValue = 0, MaxValue = 100, IntegerValue = 42, Font = new Font("Segoe UI", 14f) });
        Add("EditString", () => new TextBoxExt { Text = "Edit me", Font = new Font("Segoe UI", 14f) });

        Add("ModeComboBox", () =>
        {
            var c = new ComboBoxAdv { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            c.Items.AddRange(["Auto", "Manual", "Off"]);
            c.SelectedIndex = 0;
            return c;
        });

        Add("Label", () => new AutoLabel { Text = "Syncfusion AutoLabel", Font = new Font("Segoe UI", 14f), TextAlign = ContentAlignment.MiddleCenter });

        Add("Panel", () =>
        {
            var p = new GradientPanel { BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(new Label { Text = "GradientPanel", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
            return p;
        });

        Add("ImageDisplay", () => new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadReferenceImage("ImageDisplay"),
        });

        Add("Slider",
            () => NewTrackBar(Orientation.Horizontal),
            () => ScaledSlider());
    }

    // iocomp Slider: vertical TrackBarEx (thumb, value, events unchanged) with a "0.0".."100.0" scale painted beside it.
    // TrackBarEx has no scale of its own, so the scale is a small owner-drawn panel aligned to the track.
    private static Control ScaledSlider()
    {
        var bar = NewTrackBar(Orientation.Vertical);
        bar.ShowButtons = false;
        bar.Dock = DockStyle.Left;
        bar.Width = 26;
        bar.TrackBarGradientStart = bar.TrackBarGradientEnd = Color.White;
        bar.ButtonColor = Color.FromArgb(240, 240, 240);
        var scale = new Panel { Dock = DockStyle.Left, Width = 70 };
        scale.Paint += (_, e) =>
        {
            var g = e.Graphics;
            using var pen = new Pen(Color.Black);
            using var font = new Font("Segoe UI", 8.5f);
            var inset = bar.SliderSize.Height / 2f + 2;
            float top = inset, bottom = scale.Height - inset;
            var h = font.GetHeight(g);
            for (var v = bar.Minimum; v <= bar.Maximum; v += 4)
            {
                var y = bottom - (v - bar.Minimum) / (float)(bar.Maximum - bar.Minimum) * (bottom - top);
                var major = v % 20 == 0;
                g.DrawLine(pen, 2, y, major ? 12 : 7, y);
                if (major) g.DrawString(v.ToString("0.0"), font, Brushes.Black, 14, y - h / 2);
            }
        };
        var host = new Panel { AccessibleName = "Slider with scale" };
        host.Controls.Add(scale);
        host.Controls.Add(bar);
        return host;
    }

    private static TrackBarEx NewTrackBar(Orientation o) =>
        new() { Minimum = 0, Maximum = 100, Value = 40, Orientation = o, SmallChange = 5, LargeChange = 20 };

    private static Image? LoadReferenceImage(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "iocomp-dotnet-images", name + ".png");
        return File.Exists(path) ? Image.FromFile(path) : null;
    }
}
