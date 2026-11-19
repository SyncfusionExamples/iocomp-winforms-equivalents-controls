using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Round glossy LED indicator (prototype for the iocomp Led control).</summary>
public class Led : Control
{
    private bool _isOn = true;
    private Color _onColor = Color.Lime;
    private Color _offColor = Color.FromArgb(30, 70, 30);

    public Led()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(48, 48);
    }

    public event EventHandler? IsOnChanged;

    /// <summary>When true, a click toggles <see cref="IsOn"/>.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Interactive { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            Invalidate();
            IsOnChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OnColor
    {
        get => _onColor;
        set { _onColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OffColor
    {
        get => _offColor;
        set { _offColor = value; Invalidate(); }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Interactive) IsOn = !IsOn;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var d = Math.Min(Width, Height) - 2f;
        if (d < 6) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d);

        using (var ring = new LinearGradientBrush(rect, Color.FromArgb(244, 244, 244), Color.FromArgb(192, 192, 192), 45f))
            g.FillEllipse(ring, rect);

        var inner = RectangleF.Inflate(rect, -d * 0.1f, -d * 0.1f);
        using var path = new GraphicsPath();
        path.AddEllipse(inner);
        var baseColor = _isOn ? _onColor : _offColor;
        using var glow = new PathGradientBrush(path)
        {
            CenterPoint = new PointF(inner.Left + inner.Width * 0.33f, inner.Top + inner.Height * 0.33f),
            CenterColor = _isOn ? Color.White : Blend(baseColor, Color.White, 0.3f),
            SurroundColors = [baseColor],
        };
        g.FillEllipse(glow, inner);
    }

    private static Color Blend(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
}
