using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Shared selection logic for the multi-position switches: items, SelectedIndex, change event, keyboard.</summary>
public abstract class SelectorSwitch : Control
{
    private string[] _items;
    private int _selectedIndex;

    protected SelectorSwitch(string[] items, int selected)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        _items = items;
        _selectedIndex = selected;
    }

    public event EventHandler? SelectedIndexChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string[] Items
    {
        get => _items;
        set
        {
            _items = value is { Length: > 0 } ? value : ["Off"];
            _selectedIndex = Math.Clamp(_selectedIndex, 0, _items.Length - 1);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var v = Math.Clamp(value, 0, _items.Length - 1);
            if (v == _selectedIndex) return;
            _selectedIndex = v;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string SelectedItem => _items[_selectedIndex];

    /// <summary>Index under a client point, or -1. Public so tests and hosts can drive the switch without real mouse input.</summary>
    public abstract int HitTest(Point p);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        var i = HitTest(e.Location);
        if (i >= 0) SelectedIndex = i;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = KeyStep(e.KeyCode);
        if (step != 0) { SelectedIndex += step; e.Handled = true; }
    }

    /// <summary>Index change for an arrow key; 0 = not handled.</summary>
    protected virtual int KeyStep(Keys key) => key switch { Keys.Up or Keys.Right => 1, Keys.Down or Keys.Left => -1, _ => 0 };

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected void DrawFocus(Graphics g, Rectangle r)
    {
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, r);
    }
}

/// <summary>
/// Four-way directional switch (prototype for the iocomp SwitchQuad): a centre square with four triangular arms.
/// Items are Up, Right, Down, Left; pressing an arm selects it and the pressed arm is drawn sunken.
/// </summary>
public class SwitchQuad : SelectorSwitch
{
    public SwitchQuad() : base(["Up", "Right", "Down", "Left"], 0) => Size = new Size(128, 128);

    private (RectangleF Centre, PointF[][] Arms) Geometry()
    {
        float s = Math.Min(Width, Height) - 2, ox = (Width - s) / 2f, oy = (Height - s) / 2f;
        var c = new RectangleF(ox + s / 3, oy + s / 3, s / 3, s / 3);
        PointF P(float x, float y) => new(ox + x * s, oy + y * s);
        return (c,
        [
            [P(1 / 3f, 1 / 3f), P(0.5f, 0), P(2 / 3f, 1 / 3f)],
            [P(2 / 3f, 1 / 3f), P(1, 0.5f), P(2 / 3f, 2 / 3f)],
            [P(2 / 3f, 2 / 3f), P(0.5f, 1), P(1 / 3f, 2 / 3f)],
            [P(1 / 3f, 2 / 3f), P(0, 0.5f), P(1 / 3f, 1 / 3f)],
        ]);
    }

    public override int HitTest(Point p)
    {
        var (_, arms) = Geometry();
        for (var i = 0; i < arms.Length; i++)
        {
            using var path = new GraphicsPath();
            path.AddPolygon(arms[i]);
            if (path.IsVisible(p)) return i;
        }
        return -1;
    }

    protected override int KeyStep(Keys key) => key switch
    {
        Keys.Up => 0 - SelectedIndex, Keys.Right => 1 - SelectedIndex, Keys.Down => 2 - SelectedIndex, Keys.Left => 3 - SelectedIndex, _ => 0,
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var (centre, arms) = Geometry();
        using var arm = new SolidBrush(Color.FromArgb(170, 170, 170));
        using var pressed = new SolidBrush(Color.FromArgb(120, 120, 120));
        using var edge = new Pen(Color.FromArgb(105, 105, 105));
        using var light = new Pen(Color.White, 1.5f);
        for (var i = 0; i < arms.Length; i++)
        {
            g.FillPolygon(i == SelectedIndex ? pressed : arm, arms[i]);
            g.DrawPolygon(edge, arms[i]);
        }
        using (var body = new SolidBrush(Color.FromArgb(110, 110, 110)))
            g.FillRectangle(body, centre);
        g.DrawRectangle(light, centre.X, centre.Y, centre.Width, centre.Height);
        DrawFocus(g, Rectangle.Round(centre));
    }
}

/// <summary>
/// Panel dial switch (prototype for the iocomp SwitchPanel): black label strip with the positions,
/// a round grey knob with a yellow bar pointing at the selection. Click a label (or the knob's side) to select.
/// </summary>
public class SwitchPanel : SelectorSwitch
{
    public SwitchPanel() : base(["On", "Off", "Auto"], 0) => Size = new Size(181, 256);

    private float Strip => Height * 0.25f;

    // Positions are spread over the top arc: first at upper-left, middle straight up, last at upper-right.
    private float AngleOf(int i) => Items.Length == 1 ? -90 : -150 + i * 120f / (Items.Length - 1);

    private (PointF Centre, float Radius) Knob()
    {
        var area = new RectangleF(0, Strip, Width, Height - Strip);
        var r = Math.Min(area.Width, area.Height) * 0.42f;
        return (new PointF(area.X + area.Width / 2, area.Y + area.Height / 2), r);
    }

    private RectangleF LabelRect(int i)
    {
        // Middle item on the first row (iocomp "Off"), the others on the second row at left and right.
        var row = Strip / 2;
        if (Items.Length == 3)
            return i == 1 ? new RectangleF(0, 0, Width, row) : new RectangleF(i == 0 ? 0 : Width / 2f, row, Width / 2f, row);
        var w = Width / (float)Items.Length;
        return new RectangleF(i * w, 0, w, Strip);
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
        using (var strip = new SolidBrush(Color.Black))
            g.FillRectangle(strip, 0, 0, Width, Strip);
        using (var face = new SolidBrush(Color.FromArgb(240, 240, 240)))
            g.FillRectangle(face, 0, Strip, Width, Height - Strip);

        using var font = new Font(Font.FontFamily, Math.Max(7f, Strip * 0.3f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (var i = 0; i < Items.Length; i++)
        {
            using var text = new SolidBrush(i == SelectedIndex ? Color.Cyan : Color.White);
            g.DrawString(Items[i], font, text, LabelRect(i), sf);
        }

        var (c, r) = Knob();
        using (var rim = new LinearGradientBrush(new RectangleF(c.X - r * 1.08f, c.Y - r * 1.08f, r * 2.16f, r * 2.16f), Color.White, Color.FromArgb(200, 200, 200), 45f))
            g.FillEllipse(rim, c.X - r * 1.08f, c.Y - r * 1.08f, r * 2.16f, r * 2.16f);
        using (var knob = new SolidBrush(Color.Gray))
            g.FillEllipse(knob, c.X - r, c.Y - r, 2 * r, 2 * r);

        var a = AngleOf(SelectedIndex) * MathF.PI / 180;
        var tip = new PointF(c.X + r * 0.97f * MathF.Cos(a), c.Y + r * 0.97f * MathF.Sin(a));
        var tail = new PointF(c.X - r * 0.97f * MathF.Cos(a), c.Y - r * 0.97f * MathF.Sin(a));
        using (var bar = new Pen(Color.Yellow, r * 0.2f))
            g.DrawLine(bar, tail, tip);
        DrawFocus(g, Rectangle.Round(new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r)));
    }
}

/// <summary>
/// Multi-position slider switch (prototype for the iocomp SwitchSlider): a vertical slot with a thumb that snaps
/// to labelled positions; the selected label is drawn larger. Items run bottom (index 0) to top.
/// </summary>
public class SwitchSlider : SelectorSwitch
{
    public SwitchSlider() : base(["Low", "Medium", "High"], 2) => Size = new Size(256, 256);

    private RectangleF Slot => new(Width * 0.02f, Height * 0.04f, Math.Max(12f, Width * 0.13f), Height * 0.92f);

    private float YOf(int i) => Slot.Bottom - Slot.Width * 0.3f - i * (Slot.Height - Slot.Width * 0.6f) / Math.Max(1, Items.Length - 1);

    public override int HitTest(Point p)
    {
        var best = 0;
        for (var i = 1; i < Items.Length; i++)
            if (Math.Abs(p.Y - YOf(i)) < Math.Abs(p.Y - YOf(best))) best = i;
        return best;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button == MouseButtons.Left) SelectedIndex = HitTest(e.Location);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var slot = Slot;
        using (var white = new SolidBrush(Color.White))
            g.FillRectangle(white, slot);
        ControlPaint.DrawBorder3D(g, Rectangle.Round(slot), Border3DStyle.Sunken);

        var thumbH = slot.Width * 0.6f;
        var thumb = new RectangleF(slot.X, YOf(SelectedIndex) - thumbH / 2, slot.Width, thumbH);
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
            using var font = new Font(Font.FontFamily, i == SelectedIndex ? small * 1.6f : small, FontStyle.Regular, GraphicsUnit.Pixel);
            var h = font.GetHeight(g);
            g.DrawString(Items[i], font, text, x + 8, y - h / 2);
        }
        DrawFocus(g, Rectangle.Round(thumb));
    }
}
