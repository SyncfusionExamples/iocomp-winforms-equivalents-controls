using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Valve symbol (bow-tie body, stem and dome actuator) that toggles open/closed (prototype for the iocomp Valve).</summary>
public class Valve : Control
{
    private bool _isOpen = true;
    private Color _openColor = Color.Lime;
    private Color _closedColor = Color.Red;

    public Valve()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        Size = new Size(128, 110);
    }

    public event EventHandler? IsOpenChanged;

    /// <summary>When true (default), a click or Space/Enter toggles <see cref="IsOpen"/>. Set false for display-only use.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Interactive { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value) return;
            _isOpen = value;
            Invalidate();
            IsOpenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OpenColor
    {
        get => _openColor;
        set { _openColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color ClosedColor
    {
        get => _closedColor;
        set { _closedColor = value; Invalidate(); }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Interactive) { Focus(); IsOpen = !IsOpen; }
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Interactive && e.KeyCode is Keys.Space or Keys.Enter) IsOpen = !IsOpen;
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float w = Width - 1, h = Height - 1;
        var state = _isOpen ? _openColor : _closedColor;

        // Proportions taken from the iocomp reference image (256 x 219).
        var top = h * 0.41f;
        var mid = new PointF(w / 2f, h * 0.70f);
        var domeBase = h * 0.30f;
        var r = Math.Min(w * 0.25f, domeBase);

        // Body: flat bow-tie with a white upper edge and a grey lower edge (embossed look, no outer verticals).
        PointF[] left = [new(0, top), mid, new(0, h)];
        PointF[] right = [new(w, top), mid, new(w, h)];
        using (var bodyFill = new SolidBrush(Color.FromArgb(240, 240, 240)))
        {
            g.FillPolygon(bodyFill, left);
            g.FillPolygon(bodyFill, right);
        }
        using (var light = new Pen(Color.White))
        {
            g.DrawLine(light, left[0], mid);
            g.DrawLine(light, right[0], mid);
        }
        using (var shade = new Pen(Color.FromArgb(120, 120, 120)))
        {
            g.DrawLine(shade, left[2], mid);
            g.DrawLine(shade, right[2], mid);
        }

        // Stem: state colour with a white left edge and a dark right edge.
        var sw = Math.Max(2f, w * 0.01f);
        g.SmoothingMode = SmoothingMode.None;
        using (var stem = new SolidBrush(state))
            g.FillRectangle(stem, mid.X - sw / 2, domeBase, sw, mid.Y - domeBase);
        using (var stemLight = new Pen(Color.White))
            g.DrawLine(stemLight, mid.X - sw / 2 - 1, domeBase, mid.X - sw / 2 - 1, mid.Y);
        using (var stemDark = new Pen(Color.FromArgb(105, 105, 105)))
            g.DrawLine(stemDark, mid.X + sw / 2, domeBase, mid.X + sw / 2, mid.Y);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Dome actuator: half circle with a soft highlight up and to the left.
        using var path = new GraphicsPath();
        path.AddPie(mid.X - r, domeBase - r, 2 * r, 2 * r, 180, 180);
        using var glow = new PathGradientBrush(path)
        {
            CenterPoint = new PointF(mid.X - r * 0.25f, domeBase - r * 0.55f),
            CenterColor = Color.FromArgb(Math.Min(255, state.R + (255 - state.R) * 3 / 4), Math.Min(255, state.G + (255 - state.G) * 3 / 4), Math.Min(255, state.B + (255 - state.B) * 3 / 4)),
            SurroundColors = [state],
        };
        g.FillPath(glow, path);

        if (Focused)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, Width - 3, Height - 3));
    }
}
