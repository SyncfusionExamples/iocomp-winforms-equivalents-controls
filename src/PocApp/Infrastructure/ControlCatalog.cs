namespace PocApp.Infrastructure;

/// <summary>
/// All 56 iocomp .NET WinForms controls (one per image in iocomp-dotnet-images).
/// Ratings come from the POC; every effort figure is an estimate (person-days were never timed).
/// Approach and Fidelity describe the best live view on the card (the restyled view when there is one).
/// Fidelity is one reviewer's judgement from rendered snapshots at 100% scaling, not a measured score.
/// </summary>
public static class ControlCatalog
{
    private const Approach Props = Approach.Properties, Comp = Approach.Composition, Rend = Approach.CustomRenderer,
        Paint = Approach.CustomPaint, Proto = Approach.Prototype, None = Approach.Placeholder;

    private const Fidelity Close = Fidelity.Close, Ok = Fidelity.Acceptable, Diff = Fidelity.Different, NoView = Fidelity.None;

    private static readonly Dictionary<string, (Approach Approach, Fidelity Fidelity)> Assessment = new()
    {
        ["Plot"] = (Props, Close), ["SpectrumDisplay"] = (Props, Ok), ["PercentBar"] = (Props, Diff), ["PercentPie"] = (Props, Ok),
        ["GaugeAngular"] = (Props, Ok), ["GaugeLinear"] = (Props, Ok), ["GaugeTube"] = (Rend, Close), ["Thermometer"] = (Rend, Close),
        ["Knob"] = (Rend, Close), ["Compass"] = (Rend, Close), ["LedBar"] = (Rend, Close), ["LedSpiral"] = (Rend, Close), ["Tank"] = (Rend, Ok),
        ["SevenSegmentAnalog"] = (Props, Close), ["SevenSegmentBinary"] = (Props, Close), ["SevenSegmentCharacter"] = (Props, Close),
        ["SevenSegmentHexadecimal"] = (Props, Close), ["SevenSegmentInteger"] = (Props, Close), ["LcdMatrix"] = (Props, Ok), ["Odometer"] = (Proto, Close),
        ["ClockAnalog"] = (Rend, Ok), ["SevenSegmentClock"] = (Props, Close), ["SevenSegmentClockSmpte"] = (Props, Close),
        ["DisplayDouble"] = (Props, Diff), ["DisplayInteger"] = (Props, Diff), ["DisplayString"] = (Props, Diff),
        ["EditDouble"] = (Props, Close), ["EditInteger"] = (Props, Close), ["EditString"] = (Props, Close), ["ModeComboBox"] = (Props, Close),
        ["Label"] = (Props, Ok), ["Panel"] = (Props, Ok), ["ImageDisplay"] = (Props, Close), ["Slider"] = (Props, Diff),
        ["SwitchToggle"] = (Rend, Close), ["SwitchLed"] = (Rend, Close), ["SwitchRotary"] = (Proto, Close), ["SwitchPanel"] = (None, NoView),
        ["SwitchLever"] = (Paint, Close), ["SwitchRocker"] = (Rend, Close), ["SwitchRocker3Way"] = (Paint, Close), ["SwitchSlider"] = (Props, Diff),
        ["SwitchQuad"] = (None, NoView), ["Led"] = (Proto, Close), ["LedArrow"] = (None, NoView), ["MatrixLeds"] = (None, NoView),
        ["MatrixButtons"] = (Comp, Ok), ["PhonePad"] = (Comp, Ok), ["Pipe"] = (Proto, Close), ["PipeJoint"] = (None, NoView),
        ["Valve"] = (Proto, Close), ["Motor"] = (None, NoView), ["ObjectCanvas"] = (Props, Diff), ["SlidingScale"] = (Rend, Close),
        ["SlidingCompass"] = (Rend, Close), ["RotationDisplay"] = (None, NoView),
    };

    public static readonly string[] Categories =
        ["Plot", "Gauges", "Digital", "Clocks", "Inputs", "Switches", "Leds", "Process"];

    private static ControlEntry A(string name, string cat, string sf, string gap, string notes = "") =>
        new(name, cat, Rating.A, sf, gap, 0, 0, EffortSource.None, null, false, null, notes);

    private static ControlEntry E(string name, string cat, string sf, string gap, double min, double max,
        string prio, string notes, bool fr = false, string? frTitle = null) =>
        new(name, cat, Rating.E, sf, gap, min, max, EffortSource.Estimate, prio, fr, frTitle, notes);

    private static ControlEntry N(string name, string cat, string sf, string gap, double min, double max,
        string prio, string frTitle, string notes, bool built = false) =>
        new(name, cat, Rating.N, sf, gap, min, max, EffortSource.Estimate, prio, true, frTitle, notes, built);

    private static readonly ControlEntry[] Entries =
    [
        // Plot
        E("Plot", "Plot", "ChartControl (Line, Column, Bubble)", "Mixed series supported. Strip-chart behaviour is hand-written; heavy streaming (many series at 1000+ points/s) was slow in our test - see benchmark.", 3, 6, "P1",
            "MEASURED (Phase 3, Release build, 6 s runs, one machine): the ChartControl in Syncfusion.Chart.Windows 35.2.12 has no FastLine series type (Line used). Line series, 2000-point window: 1 series at 1000 pts/s gave ~21 fps (~32 with ImprovePerformance); 8 series at 1000 pts/s gave ~2.6 fps (~7 with ImprovePerformance); 4 or 8 series at 10000 pts/s could not keep up (1.3 and 0.8 fps, and in one run the UI stalled for ~9 s). See docs/benchmark_*.csv. Mixed line/column/bubble series and the dark restyle work. Strip-chart behaviour here is hand-written (Points.Add plus RemoveAt(0)); sweep mode is not implemented.", true, "Strip-chart / sweep mode for ChartControl"),
        A("SpectrumDisplay", "Plot", "ChartControl (column series)", "Feed FFT output as data.", "VERIFIED (Phase 3): column series redrawn each tick works with a black background. Default X axis ran -10..40 for 32 bars; fixed with an explicit range, but the tick labels (-0.5, 7.5, 15.5) are not clean."),
        E("PercentBar", "Plot", "ChartControl (stacked column)", "Chart configured as a percent bar.", 0.5, 1, "P3", "VERIFIED (Phase 6): both views work. Pitfall found: for the horizontal bar types (Bar, StackingBar, StackingBar100) the PrimaryXAxis becomes the horizontal VALUE axis, so it must be ValueType.Double with a 0-100 range; leaving it as Category (as for columns) squashes every bar into the right-hand corner with no error. Worth a doc note or a clearer default. Look: iocomp shows a single vertical 3D column with a side legend; the demo shows quarterly stacked bars, so the look is different (function is equivalent)."),
        A("PercentPie", "Plot", "ChartControl (pie)", "Pie series with legend.", "VERIFIED (Phase 3): works. Legend shows pie-slice symbols and the slices are flat, unlike iocomp's shaded pie."),

        // Gauges
        A("GaugeAngular", "Gauges", "RadialGauge", "Ranges, ticks and needle are supported.", "VERIFIED (Phase 2, restyled in gauge pass): properties only. QuarterCircle, ranges placed OutSide with ticks inside them, DrawLabel event gives \"0.0\" labels. Remaining differences: the needle is the short Advanced style, not iocomp's long tapered bar along the bottom, and the top labels crowd at this size."),
        A("GaugeLinear", "Gauges", "LinearGauge", "Scale, ranges and pointer are supported."),
        E("GaugeTube", "Gauges", "LinearGauge (restyled)", "Glass tube frame needs custom styling.", 1, 2, "P3", "VERIFIED (gauge pass): restyled view uses a custom ILinearGaugeRenderer (Cards/GaugeRenderers.cs, ~60 lines shared with Thermometer) for the glass tube, rim, liquid column and \"0.0\" scale. Close to iocomp. Properties alone cannot draw the tube."),
        E("Thermometer", "Gauges", "LinearGauge (restyled)", "Bulb shape needs custom styling.", 1, 2, "P2", "VERIFIED (gauge pass): restyled view uses the same custom ILinearGaugeRenderer as GaugeTube with a bulb. Close to iocomp. Properties alone cannot draw the bulb."),
        E("Knob", "Gauges", "RadialGauge (+ mouse-drag pointer)", "Interactive pointer and glossy cap need custom code.", 2, 4, "P1",
            "PARTLY VERIFIED (Phase 2): ReadOnly=false renders a pointer-style gauge, but it does not look like a knob (no glossy cap). INTERACTION TESTED (Phase 6, synthetic mouse messages): clicking sets the value from the click angle (e.g. 3 o'clock gave 83.3, 9 o'clock gave 16.7) and ValueChanged fires. Clicks in the gap below the arc gave 0, and a scripted drag ended at 0 (its end point fell in that gap), so smooth drag tracking is not confirmed. Real-mouse feel not judged. Gauge pass: restyled view now paints a glossy knob, red dot, ticks and \"0.0\" labels through a custom IRadialGaugeRenderer; mouse input is still the gauge's own (not re-tested).", true, "Interactive (draggable) pointer on RadialGauge"),
        E("Compass", "Gauges", "RadialGauge (0-360, text labels)", "Numeric 0-360 scale only; N/E/S/W labels and centre readout need custom code.", 1.5, 3, "P2", "VERIFIED (Phase 2): scale labels are numbers only (0-360). Gauge pass: restyled view paints the navy ring, N/NE/E... labels, glossy centre with heading and yellow pointer through a custom IRadialGaugeRenderer. Close to iocomp, but only with custom drawing, so the FR stands.", true, "Custom text labels on RadialGauge scale"),
        E("LedBar", "Gauges", "LinearGauge (segmented style)", "Segmented look via ticks/ranges.", 1.5, 3, "P2", "VERIFIED (Phase 2): no segmented mode. Gauge pass: restyled view is a vertical segmented bar (unlit segments dimmed, colours from Ranges) through a custom ILinearGaugeRenderer. FR confirmed.", true, "Segmented (LED-style) bar mode for LinearGauge"),
        E("LedSpiral", "Gauges", "RadialGauge (segmented style)", "Segmented ring via ranges; may need custom drawing.", 2, 4, "P3", "VERIFIED (Phase 2): ranges are continuous arcs, not LED segments. Gauge pass: restyled view is a 50-segment ring (unlit segments dimmed, colours from Ranges) through a custom IRadialGaugeRenderer. Close to iocomp."),
        N("Tank", "Gauges", "LinearGauge + custom renderer (stand-in)", "No vessel/liquid-fill control.", 3, 5, "P2", "Tank / vessel level indicator", "Process-graphics gap. Stand-in pass: LinearGauge with a custom ILinearGaugeRenderer (TankRenderer, ~30 lines) draws the silver capsule and a blue level that follows Value. Rectangular interior only: iocomp's free-form vessel outline is not reproduced."),

        // Digital
        E("SevenSegmentAnalog", "Digital", "DigitalGauge + formatting", "Format by code.", 0.5, 1, "P3", "VERIFIED (Phase 2): unlit segments work via ShowInvisibleSegments plus InactiveTextColor in a custom theme brush."),
        E("SevenSegmentBinary", "Digital", "DigitalGauge + formatting", "Format by code.", 0.5, 1, "P3", "VERIFIED (digital pass, render only): Convert.ToString(v, 2) padded to 6 digits with the iocomp palette. Close."),
        E("SevenSegmentCharacter", "Digital", "DigitalGauge + formatting", "Depends on supported character set.", 0.5, 1, "P3", "VERIFIED (Phase 2): 7-segment cannot draw all letters; FourteenSegment renders them clearly."),
        E("SevenSegmentHexadecimal", "Digital", "DigitalGauge + formatting", "Format by code.", 0.5, 1, "P3", "VERIFIED (digital pass, render only): ToString(\"X6\") with the iocomp palette. Close."),
        A("SevenSegmentInteger", "Digital", "DigitalGauge", "Integer display with segment style.", "VERIFIED (Phase 2): works. The '+' sign shown by iocomp is not built in; add it to the text. Digital pass: iocomp palette applied with a custom DigitalGaugeThemeBrush (black face, 00FF00 lit, 007F00 unlit, square corners)."),
        E("LcdMatrix", "Digital", "DigitalGauge (dot-matrix)", "Dot-matrix character type is available; upper-case text and digits tested.", 1, 2, "P3", "VERIFIED (Phase 2): CharacterType.DotMatrixSegment works with upper-case text and digits. Digital pass: teal face with cyan dots via theme brush. iocomp's single-row lower-case text on a full dot field is not reproduced (DigitalGauge draws character cells)."),
        N("Odometer", "Digital", "DigitalGauge (stand-in)", "No rolling-digit display.", 2, 4, "P2", "Rolling-digit odometer display", "BUILT (Phase 4): HmiControls.Odometer, 131 lines (after the look pass). Animated drum roll up and down, RollCompleted event, metallic look similar to iocomp's. Carry-over between drums is simplified (higher drums roll only as the lower one wraps). Scripted test passed; effort stays an estimate.", built: true),

        // Clocks
        E("ClockAnalog", "Clocks", "Clock (restyled)", "Hand colours and a dark style are available; the face is a plain clock face.", 1, 2, "P3", "VERIFIED (Phase 2): hands, colours and a dark style work. The clock draws a square shadow behind the round face and does not size itself to its container (fixed size used). Possible bug to report. Clock pass: restyled view uses Clock.Renderer with a custom IClockRenderer (tapered shaded hands, 1-12 numerals) on a grey circular face. Close, but the square shadow still shows inside the face even with a round Region."),
        E("SevenSegmentClock", "Clocks", "DigitalGauge + timer", "Time formatting by code.", 0.5, 1, "P3", "VERIFIED (clock pass, render only): HH:mm:ss with the iocomp palette. Pitfall: DigitalGauge counts ':' as a character cell, so CharacterCount must include the separators or the text is cut off."),
        E("SevenSegmentClockSmpte", "Clocks", "DigitalGauge + timer", "Frame-rate formatting by code.", 0.5, 1, "P3", "VERIFIED (clock pass, render only): HH:mm:ss:ff at 25 fps in 11 cells. iocomp also shows hundredths; adding them needs a wider gauge."),

        // Inputs
        A("DisplayDouble", "Inputs", "Label / TextBoxExt (read-only)", "Alarm colours and formatting by code.", "Function equivalent. Look differs: iocomp default is cyan text on black; the demo keeps the Syncfusion default (one BackColor/ForeColor change away)."),
        A("DisplayInteger", "Inputs", "Label / TextBoxExt (read-only)", "Alarm colours and formatting by code.", "As DisplayDouble: look differs only by default colours."),
        A("DisplayString", "Inputs", "Label / TextBoxExt (read-only)", "Read-only text.", "As DisplayDouble: look differs only by default colours."),
        A("EditDouble", "Inputs", "DoubleTextBox", "Min/max validation is built in."),
        A("EditInteger", "Inputs", "IntegerTextBox", "Min/max validation is built in."),
        A("EditString", "Inputs", "TextBoxExt", "Text entry."),
        A("ModeComboBox", "Inputs", "ComboBoxAdv", "Drop-down selection."),
        A("Label", "Inputs", "Label / AutoLabel", "Text label."),
        A("Panel", "Inputs", "GradientPanel", "Titled container."),
        A("ImageDisplay", "Inputs", "PictureBox", "Static image."),
        E("Slider", "Inputs", "TrackBarEx (restyled)", "No numeric tick labels or scale on the slider today.", 0.5, 1, "P3", "VERIFIED (Phase 2): TrackBarEx has no numeric tick labels or scale (iocomp's slider has both). Likely FR. Mouse click moves the thumb (10 -> 15 in a synthetic-click test).", true, "Tick labels / scale on TrackBarEx"),

        // Switches
        E("SwitchToggle", "Switches", "ToggleButton", "Visual style differs.", 0.5, 1, "P3", "VERIFIED (Phase 5, render only): ToggleButton default and Office2016Colorful styles both render and differ clearly. Neither matches iocomp's flat two-state switch. Click tested with synthetic mouse messages: toggles Inactive -> Active -> Inactive and raises ToggleStateChanged each time. Switch pass: restyled view is ToggleButton + custom IToggleButtonRenderer (ridged paddle up/down). Close to iocomp; render checked, click not re-tested."),
        E("SwitchLed", "Switches", "ToggleButton + IToggleButtonRenderer", "Label with coloured indicator by styling.", 0.5, 1, "P3", "PARTLY VERIFIED (Phase 5): ButtonAdv with a toggled caption and back colour works, but iocomp's big caption with a separate coloured indicator bar is not reproduced. ButtonAdv.Click fires on a synthetic mouse click. Switch pass: restyled view is now ToggleButton + custom IToggleButtonRenderer: big ON/OFF caption and green indicator bar in a raised face. Close to iocomp."),
        N("SwitchRotary", "Switches", "(none)", "No multi-position rotary switch.", 3, 5, "P1", "Rotary selector switch", "BUILT (Phase 4): HmiControls.SwitchRotary, 230 lines after the look pass (largest of the five). Leader-line labels, pointer bar, mouse position and arrow-key selection, SelectedIndexChanged. Scripted test passed, but mouse input was tested by calling SelectAt directly, not with real mouse events. Not themed or accessibility-reviewed. Effort stays an estimate.", built: true),
        N("SwitchPanel", "Switches", "(none)", "No panel-dial switch.", 3, 5, "P2", "Panel dial switch", ""),
        N("SwitchLever", "Switches", "CheckBoxAdv + custom paint (stand-in)", "No lever switch.", 3, 5, "P2", "Lever switch", "Stand-in pass: CheckBoxAdv keeps Checked/CheckedChanged and click/Space toggling; OnPaint overridden (CheckBoxAdv has no renderer interface) to draw the ball-and-shaft lever, up = on. Close to iocomp at rest; no lever animation."),
        N("SwitchRocker", "Switches", "ToggleButton + IToggleButtonRenderer (stand-in)", "No rocker switch.", 2, 4, "P2", "Rocker switch", "Stand-in pass: ToggleButton with a custom IToggleButtonRenderer draws the lamp window (lit when Active) and ridged rocker face. Close to iocomp."),
        N("SwitchRocker3Way", "Switches", "CheckBoxAdv (Tristate) + custom paint (stand-in)", "No three-way rocker switch.", 2, 4, "P2", "Three-way rocker switch", "Stand-in pass: CheckBoxAdv with Tristate gives three states (Checked = up, Indeterminate = centre, Unchecked = down) and CheckStateChanged; OnPaint draws the two-paddle rocker. Click cycles states in CheckBoxAdv order, not by which half was pressed."),
        N("SwitchSlider", "Switches", "TrackBarEx (stand-in)", "No labelled multi-position slider switch.", 2, 3, "P2", "Multi-position slider switch", "Stand-in: 3-position vertical TrackBarEx with a text readout. No position labels or switch body; look is different. Next candidate for custom painting."),
        N("SwitchQuad", "Switches", "(none)", "No four-way directional switch.", 2, 4, "P3", "Four-way directional switch", ""),

        // Leds
        N("Led", "Leds", "(none)", "No LED indicator.", 1, 2, "P1", "LED indicator control", "BUILT (Phase 4): HmiControls.Led, 86 lines. Glossy gradient look close to iocomp's; IsOn, colours, click-toggle, IsOnChanged. Behaviour checked by a scripted test (events, toggle); real mouse not tried. Effort stays an estimate: person-days were not measured.", built: true),
        N("LedArrow", "Leds", "(none)", "No arrow indicator.", 1, 2, "P3", "Arrow indicator control", ""),
        N("MatrixLeds", "Leds", "(none)", "No LED matrix.", 2, 3, "P3", "LED matrix control", ""),
        E("MatrixButtons", "Leds", "ButtonAdv grid in TableLayoutPanel", "Composition, not a drop-in.", 1, 2, "P3", "VERIFIED (Phase 5, render only): the grid is easy to build. The demo uses 4x4; iocomp's image is 3 columns x 4 rows. No row/column button events or selection model - the customer would write those."),
        E("PhonePad", "Leds", "ButtonAdv grid in TableLayoutPanel", "Composition, not a drop-in.", 1, 2, "P3", "VERIFIED (Phase 5, render only): 4x3 keypad matches the iocomp layout. Key press handling is not wired."),

        // Process
        N("Pipe", "Process", "(none)", "No animated pipe.", 3, 6, "P1", "Process pipe with animated flow", "BUILT (Phase 4): HmiControls.Pipe, 126 lines (after the look pass). Timer-driven flow arrows with direction, speed and on/off; scripted test confirmed arrows move only while flowing. Straight horizontal pipe only: no vertical pipes, elbows or PipeJoint. Animation cost with many pipes is untested. Effort stays an estimate.", built: true),
        N("PipeJoint", "Process", "(none)", "No pipe joint.", 1, 2, "P2", "Pipe joint symbol", ""),
        N("Valve", "Process", "(none)", "No valve symbol.", 3, 5, "P1", "Process valve symbol", "BUILT (Phase 4): HmiControls.Valve, 129 lines (after the look pass). Bow-tie body, stem and dome, open/closed colours, click and Space/Enter toggle, IsOpenChanged; the demo wires it to the outlet Pipe. Scripted test passed. Look pass: open bow-tie body, shaded stem and dome redrawn from iocomp pixel samples; close. Effort stays an estimate.", built: true),
        N("Motor", "Process", "(none)", "No motor symbol.", 3, 5, "P2", "Motor symbol with running state", ""),
        E("ObjectCanvas", "Process", "Diagram (shapes, text, rotation)", "Drawing surface via Diagram.", 2, 4, "P2", "VERIFIED (Phase 5, render only): Diagram renders rectangles from code and Rotate(20) tilts a shape. The text, spinner and runtime-driven animation shown in iocomp's image were NOT tried. Diagram is a heavier control than a simple canvas."),
        N("SlidingScale", "Process", "LinearGauge + custom renderer (stand-in)", "No scrolling tape scale.", 3, 5, "P2", "Scrolling tape scale gauge", "Stand-in pass: LinearGauge holds Value/Min/Max/MajorDifference; a custom ILinearGaugeRenderer (SlidingTapeRenderer, shared with SlidingCompass) scrolls the shaded tape under a fixed blue line with yellow markers. Close to iocomp. Display only: no drag-to-scroll."),
        N("SlidingCompass", "Process", "LinearGauge + custom renderer (stand-in)", "No scrolling heading tape.", 3, 5, "P2", "Scrolling heading tape gauge", "Stand-in pass: same SlidingTapeRenderer with labels wrapping at 360 (340, 0, 20). Close to iocomp. Display only."),
        N("RotationDisplay", "Process", "(none)", "Needs custom drawing at an arbitrary angle.", 1, 2, "P3", "Rotated image/text display", ""),
    ];

    // Every entry must have an assessment; a missing name fails at startup rather than silently showing "Unverified".
    public static readonly IReadOnlyList<ControlEntry> All = Entries
        .Select(e => e with { Approach = Assessment[e.Name].Approach, Fidelity = Assessment[e.Name].Fidelity })
        .ToList();
}
