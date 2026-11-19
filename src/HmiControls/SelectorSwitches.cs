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
