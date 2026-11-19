using System.ComponentModel;
using System.Drawing.Drawing2D;
using Syncfusion.Windows.Forms.Tools;

namespace PocApp.Cards;

/// <summary>
/// Three-position selector switch on a tristate CheckBoxAdv. CheckBoxAdv keeps the state and raises CheckStateChanged;
/// subclasses only paint and hit-test (OnPaint overridden, as CheckBoxAdv has no renderer interface).
/// Position 0 = Unchecked, 1 = Indeterminate, 2 = Checked. Exactly three positions: that is all CheckState can hold,
/// while iocomp allows any number. AutoCheck is off, so a click selects the position under the mouse instead of cycling.
/// </summary>
internal abstract class ThreePositionBox : CheckBoxAdv
{
    private string[] _items;

    protected ThreePositionBox(string[] items, int selected)
    {
        _items = items;
        Tristate = true;
        AutoCheck = false;
        Text = "";
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        SelectedIndex = selected;
    }

    /// <summary>Labels for positions 0, 1 and 2.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string[] Items
    {
        get => _items;
        set
        {
            if (value is not { Length: 3 }) throw new ArgumentException("A tristate switch has exactly three positions.", nameof(value));
            _items = value;
            Invalidate();
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => CheckState switch { CheckState.Unchecked => 0, CheckState.Indeterminate => 1, _ => 2 };
        set => CheckState = Math.Clamp(value, 0, 2) switch { 0 => CheckState.Unchecked, 1 => CheckState.Indeterminate, _ => CheckState.Checked };
    }

    public string SelectedItem => _items[SelectedIndex];

    /// <summary>Position that pressing at a client point selects, or -1.</summary>
    public abstract int HitTest(Point p);

    /// <summary>Whether dragging with the left button keeps selecting the position under the mouse.</summary>
    protected virtual bool SelectWhileDragging => false;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        SelectAt(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (SelectWhileDragging && e.Button == MouseButtons.Left) SelectAt(e.Location);
    }

    private void SelectAt(Point p)
    {
        var i = HitTest(p);
        if (i >= 0) SelectedIndex = i;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step = e.KeyCode switch { Keys.Up or Keys.Right => 1, Keys.Down or Keys.Left => -1, _ => 0 };
        if (step != 0) { SelectedIndex += step; e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    protected void FillBackground(Graphics g)
    {
        using var back = new SolidBrush(Parent?.BackColor ?? SystemColors.Control);
        g.FillRectangle(back, ClientRectangle);
    }

    protected void DrawFocus(Graphics g, Rectangle r)
    {
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, r);
    }
}

/// <summary>
/// iocomp SwitchRocker3Way look: two paddles, the pressed one sunken. Positions are Down, Centre, Up.
/// Like a real rocker, pressing a half moves one step toward it (down -> centre -> up, or the reverse), so
/// <see cref="HitTest"/> depends on the current position as well as the point.
/// </summary>
internal sealed class Rocker3WayBox : ThreePositionBox
{
    public Rocker3WayBox() : base(["Down", "Centre", "Up"], 1) => Size = new Size(70, 130);

    private Rectangle Body => SwitchDraw.Fit(ClientRectangle, 138f / 256);

    public override int HitTest(Point p)
    {
        var r = Body;
        return Math.Clamp(SelectedIndex + (p.Y < r.Top + r.Height / 2 ? 1 : -1), 0, 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        FillBackground(g);
        var r = Body;
        using (var outline = new Pen(Color.Black))
            g.DrawRectangle(outline, r.X, r.Y, r.Width - 1, r.Height - 1);
        var inner = Rectangle.Inflate(r, -2, -2);
        var half = inner.Height / 2;
        var up = SelectedIndex == 2;
        var down = SelectedIndex == 0;
        var pressed = Color.FromArgb(180, 180, 180);
        var raised = Color.FromArgb(247, 247, 247);
        using (var top = new SolidBrush(up ? pressed : raised))
            g.FillRectangle(top, inner.X, inner.Y, inner.Width, half);
        using (var bottom = new SolidBrush(down ? pressed : raised))
            g.FillRectangle(bottom, inner.X, inner.Y + half, inner.Width, inner.Height - half);
        SwitchDraw.Bevel(g, new Rectangle(inner.X, inner.Y, inner.Width, half), !up);
        SwitchDraw.Bevel(g, new Rectangle(inner.X, inner.Y + half, inner.Width, inner.Height - half), !down);
        DrawFocus(g, Rectangle.Inflate(r, -4, -4));
    }
}

/// <summary>iocomp SwitchRotary look: pointer bar through a pivot, leader-line labels, selected label drawn larger.</summary>
internal sealed class RotaryBox : ThreePositionBox
{
    private float _startAngle = 180;
    private float _sweepAngle = 90;
    private PointF _pivot;

    public RotaryBox() : base(["Low", "Medium", "High"], 1) => Size = new Size(256, 190);

    /// <summary>Angle of the first position in degrees, clockwise from 3 o'clock.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float StartAngle
    {
        get => _startAngle;
        set { _startAngle = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float SweepAngle
    {
        get => _sweepAngle;
        set { _sweepAngle = value; Invalidate(); }
    }

    protected override bool SelectWhileDragging => true;

    private float AngleOf(int i) => _startAngle + _sweepAngle * i / (Items.Length - 1);

    /// <summary>The position whose angle is closest to the direction of <paramref name="p"/> from the pivot.</summary>
    public override int HitTest(Point p)
    {
        var angle = Math.Atan2(p.Y - _pivot.Y, p.X - _pivot.X) * 180 / Math.PI;
        var best = 0;
        var bestDiff = double.MaxValue;
        for (var i = 0; i < Items.Length; i++)
        {
            var diff = Math.Abs(((angle - AngleOf(i) + 540) % 360) - 180);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }

    private readonly record struct LabelLayout(PointF Dot, PointF Elbow, PointF End, RectangleF Text, StringAlignment Align);

    // Layout relative to a pivot at (0,0); the caller centres the result in the control.
    private LabelLayout[] LayoutLabels(Graphics g, float size, Font normal, Font big, int selected, out RectangleF bounds)
    {
        var r = size * 0.34f;
        var labels = new LabelLayout[Items.Length];
        var sel = AngleOf(selected) * Math.PI / 180;
        var tail = new PointF((float)-Math.Cos(sel) * r * 1.15f, (float)-Math.Sin(sel) * r * 1.15f);
        bounds = RectangleF.FromLTRB(Math.Min(0, tail.X), Math.Min(0, tail.Y), Math.Max(0, tail.X), Math.Max(0, tail.Y));

        for (var i = 0; i < Items.Length; i++)
        {
            var a = AngleOf(i) * Math.PI / 180;
            var cos = (float)Math.Cos(a);
            var sin = (float)Math.Sin(a);
            var dot = new PointF(r * cos, r * sin);
            var ts = g.MeasureString(Items[i], i == selected ? big : normal);
            PointF elbow, end;
            RectangleF text;
            StringAlignment align;

            if (Math.Abs(cos) < 0.35f)
            {
                // Near vertical: a radial leader with the label centred beyond it (iocomp "High").
                elbow = end = new PointF(dot.X, dot.Y + sin * size * 0.09f);
                var y = sin < 0 ? end.Y - 2 - ts.Height : end.Y + 2;
                text = new RectangleF(end.X - ts.Width / 2, y, ts.Width, ts.Height);
                align = StringAlignment.Center;
            }
            else
            {
                // Short radial leg, then a horizontal leg to the label (iocomp "Medium" / "Low").
                var sign = cos >= 0 ? 1 : -1;
                elbow = new PointF(dot.X + cos * size * 0.06f, dot.Y + sin * size * 0.06f);
                end = new PointF(elbow.X + sign * size * 0.06f, elbow.Y);
                text = sign > 0
                    ? new RectangleF(end.X + 2, end.Y - ts.Height / 2, ts.Width, ts.Height)
                    : new RectangleF(end.X - 2 - ts.Width, end.Y - ts.Height / 2, ts.Width, ts.Height);
                align = sign > 0 ? StringAlignment.Near : StringAlignment.Far;
            }

            labels[i] = new LabelLayout(dot, elbow, end, text, align);
            bounds = RectangleF.Union(bounds, text);
            bounds = RectangleF.Union(bounds, new RectangleF(dot.X - 3, dot.Y - 3, 6, 6));
        }
        return labels;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        FillBackground(g);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        var size = (float)Math.Min(Width, Height);
        if (size < 20) return;
        var selected = SelectedIndex;

        // Lay out once, shrink if the labels do not fit, then centre the whole drawing.
        LabelLayout[] labels;
        RectangleF bounds;
        Font normal, big;
        while (true)
        {
            normal = new Font(Font.FontFamily, Math.Max(7f, size * 0.1f), FontStyle.Regular, GraphicsUnit.Pixel);
            big = new Font(Font.FontFamily, Math.Max(8f, size * 0.15f), FontStyle.Regular, GraphicsUnit.Pixel);
            labels = LayoutLabels(g, size, normal, big, selected, out bounds);
            // Fit and centre on the bounds of every possible selection, so the pivot stays put when the selection
            // changes. Otherwise the drawing shifts under a held mouse, the same point then maps to another
            // position, and the selection flips back and forth on every repaint (flicker).
            for (var i = 0; i < Items.Length; i++)
            {
                if (i == selected) continue;
                LayoutLabels(g, size, normal, big, i, out var other);
                bounds = RectangleF.Union(bounds, other);
            }
            var fit = Math.Min((Width - 6) / bounds.Width, (Height - 6) / bounds.Height);
            if (fit >= 1 || size < 30) break;
            normal.Dispose();
            big.Dispose();
            size *= Math.Max(0.5f, fit);
        }

        using (normal)
        using (big)
        {
            _pivot = new PointF(Width / 2f - (bounds.Left + bounds.Width / 2), Height / 2f - (bounds.Top + bounds.Height / 2));
            g.TranslateTransform(_pivot.X, _pivot.Y);

            using var line = new Pen(Color.FromArgb(40, 40, 40));
            using var dotBrush = new SolidBrush(Color.Cyan);
            using var textBrush = new SolidBrush(ForeColor);
            for (var i = 0; i < labels.Length; i++)
            {
                var l = labels[i];
                g.DrawLine(line, l.Dot, l.Elbow);
                g.DrawLine(line, l.Elbow, l.End);
                using var sf = new StringFormat { Alignment = l.Align, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(Items[i], i == selected ? big : normal, textBrush, l.Text, sf);
            }

            // Pointer bar: tip at the selected position's dot, through the pivot, tail slightly longer (as iocomp).
            var sel = AngleOf(selected) * Math.PI / 180;
            var dir = new PointF((float)Math.Cos(sel), (float)Math.Sin(sel));
            var r = labels[selected].Dot;
            var len = (float)Math.Sqrt(r.X * r.X + r.Y * r.Y);
            var p1 = new PointF(dir.X * len, dir.Y * len);
            var p2 = new PointF(-p1.X * 1.15f, -p1.Y * 1.15f);
            var half = Math.Max(2f, size * 0.026f);
            var nx = -dir.Y * half;
            var ny = dir.X * half;
            PointF[] bar = [new(p1.X + nx, p1.Y + ny), new(p2.X + nx, p2.Y + ny), new(p2.X - nx, p2.Y - ny), new(p1.X - nx, p1.Y - ny)];
            // Rectangle + angle, not two points: at 180 deg the points differ only by floating-point noise in X,
            // and GDI+ rejects that near-degenerate gradient (the paint error then closes the app).
            var shade = new RectangleF(-half, -half, 2 * half, 2 * half);
            using (var fill = new LinearGradientBrush(shade, Color.White, Color.FromArgb(236, 236, 236), AngleOf(selected) - 90f))
                g.FillPolygon(fill, bar);
            using (var outline = new Pen(Color.FromArgb(160, 160, 160)))
                g.DrawPolygon(outline, bar);

            foreach (var l in labels)
                g.FillEllipse(dotBrush, l.Dot.X - 2.5f, l.Dot.Y - 2.5f, 5, 5);

            g.ResetTransform();
        }

        DrawFocus(g, new Rectangle(1, 1, Width - 3, Height - 3));
    }
}

/// <summary>
/// iocomp SwitchPanel look: black label strip with the positions, a round grey knob with a yellow bar pointing at
/// the selection. Click a label (or the knob's side) to select.
/// </summary>
internal sealed class PanelDialBox : ThreePositionBox
{
    public PanelDialBox() : base(["On", "Off", "Auto"], 0) => Size = new Size(181, 256);

    private float Strip => Height * 0.25f;

    // Positions are spread over the top arc: first at upper-left, middle straight up, last at upper-right.
    private static float AngleOf(int i) => -150 + i * 60f;

    private (PointF Centre, float Radius) Knob()
    {
        var area = new RectangleF(0, Strip, Width, Height - Strip);
        var r = Math.Min(area.Width, area.Height) * 0.42f;
        return (new PointF(area.X + area.Width / 2, area.Y + area.Height / 2), r);
    }

    // Middle item on the first row (iocomp "Off"), the others on the second row at left and right.
    private RectangleF LabelRect(int i)
    {
        var row = Strip / 2;
        return i == 1 ? new RectangleF(0, 0, Width, row) : new RectangleF(i == 0 ? 0 : Width / 2f, row, Width / 2f, row);
    }

    public override int HitTest(Point p)
    {
        for (var i = 0; i < Items.Length; i++)
            if (LabelRect(i).Contains(p)) return i;
        var (c, r) = Knob();
        var dx = p.X - c.X;
        var dy = p.Y - c.Y;
        if (dx * dx + dy * dy > r * r) return -1;
        var a = MathF.Atan2(dy, dx) * 180 / MathF.PI;
        var best = 0;
        for (var i = 1; i < Items.Length; i++)
            if (Math.Abs(Delta(a, AngleOf(i))) < Math.Abs(Delta(a, AngleOf(best)))) best = i;
        return best;
    }

    private static float Delta(float a, float b) => ((a - b) % 360 + 540) % 360 - 180;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var selected = SelectedIndex;
        using (var strip = new SolidBrush(Color.Black))
            g.FillRectangle(strip, 0, 0, Width, Strip);
        using (var face = new SolidBrush(Color.FromArgb(240, 240, 240)))
            g.FillRectangle(face, 0, Strip, Width, Height - Strip);

        using var font = new Font(Font.FontFamily, Math.Max(7f, Strip * 0.3f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (var i = 0; i < Items.Length; i++)
        {
            using var text = new SolidBrush(i == selected ? Color.Cyan : Color.White);
            g.DrawString(Items[i], font, text, LabelRect(i), sf);
        }

        var (c, r) = Knob();
        using (var rim = new LinearGradientBrush(new RectangleF(c.X - r * 1.08f, c.Y - r * 1.08f, r * 2.16f, r * 2.16f), Color.White, Color.FromArgb(200, 200, 200), 45f))
            g.FillEllipse(rim, c.X - r * 1.08f, c.Y - r * 1.08f, r * 2.16f, r * 2.16f);
        using (var knob = new SolidBrush(Color.Gray))
            g.FillEllipse(knob, c.X - r, c.Y - r, 2 * r, 2 * r);

        var a = AngleOf(selected) * MathF.PI / 180;
        var tip = new PointF(c.X + r * 0.97f * MathF.Cos(a), c.Y + r * 0.97f * MathF.Sin(a));
        var tail = new PointF(c.X - r * 0.97f * MathF.Cos(a), c.Y - r * 0.97f * MathF.Sin(a));
        using (var bar = new Pen(Color.Yellow, r * 0.2f))
            g.DrawLine(bar, tail, tip);
        DrawFocus(g, Rectangle.Round(new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r)));
    }
}

/// <summary>
/// iocomp SwitchSlider look: a vertical slot with a thumb that snaps to labelled positions; the selected label is
/// drawn larger. Positions run bottom (0) to top.
/// </summary>
internal sealed class SliderSwitchBox : ThreePositionBox
{
    public SliderSwitchBox() : base(["Low", "Medium", "High"], 2) => Size = new Size(256, 256);

    protected override bool SelectWhileDragging => true;

    private RectangleF Slot => new(Width * 0.02f, Height * 0.04f, Math.Max(12f, Width * 0.13f), Height * 0.92f);

    private float YOf(int i) => Slot.Bottom - Slot.Width * 0.3f - i * (Slot.Height - Slot.Width * 0.6f) / (Items.Length - 1);

    public override int HitTest(Point p)
    {
        var best = 0;
        for (var i = 1; i < Items.Length; i++)
            if (Math.Abs(p.Y - YOf(i)) < Math.Abs(p.Y - YOf(best))) best = i;
        return best;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        FillBackground(g);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var selected = SelectedIndex;
        var slot = Slot;
        using (var white = new SolidBrush(Color.White))
            g.FillRectangle(white, slot);
        ControlPaint.DrawBorder3D(g, Rectangle.Round(slot), Border3DStyle.Sunken);

        var thumbH = slot.Width * 0.6f;
        var thumb = new RectangleF(slot.X, YOf(selected) - thumbH / 2, slot.Width, thumbH);
        using (var face = new SolidBrush(Color.FromArgb(240, 240, 240)))
            g.FillRectangle(face, thumb);
        ControlPaint.DrawBorder3D(g, Rectangle.Round(thumb), Border3DStyle.Raised);

        using var dot = new SolidBrush(Color.FromArgb(64, 224, 208));
        using var text = new SolidBrush(ForeColor);
        var small = Math.Max(8f, Height * 0.075f);
        for (var i = 0; i < Items.Length; i++)
        {
            var y = YOf(i);
            var x = slot.Right + slot.Width * 0.35f;
            g.FillEllipse(dot, x - 2, y - 2, 4, 4);
            using var font = new Font(Font.FontFamily, i == selected ? small * 1.6f : small, FontStyle.Regular, GraphicsUnit.Pixel);
            var h = font.GetHeight(g);
            g.DrawString(Items[i], font, text, x + 8, y - h / 2);
        }
        DrawFocus(g, Rectangle.Round(thumb));
    }
}
