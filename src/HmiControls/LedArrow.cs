using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

public enum ArrowDirection { Right, Left, Up, Down }

/// <summary>Flat arrow indicator, lit or unlit (prototype for the iocomp LedArrow).</summary>
public class LedArrow : Control
{
    private bool _isOn = true;
    private Color _onColor = Color.Green;
    private Color _offColor = Color.FromArgb(0, 64, 0);
    private ArrowDirection _direction = ArrowDirection.Right;

    public LedArrow()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(128, 64);
    }

    public event EventHandler? IsOnChanged;

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
    public Color OnColor { get => _onColor; set { _onColor = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OffColor { get => _offColor; set { _offColor = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ArrowDirection Direction { get => _direction; set { _direction = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var horizontal = _direction is ArrowDirection.Right or ArrowDirection.Left;
        // Arrow is 2:1 (iocomp 256 x 128); fit it and draw it pointing right, then rotate.
        float len = horizontal ? Math.Min(Width - 2, (Height - 2) * 2f) : Math.Min(Height - 2, (Width - 2) * 2f);
        if (len < 8) return;
        float h = len / 2;
        var state = g.Save();
        g.TranslateTransform(Width / 2f, Height / 2f);
        g.RotateTransform(_direction switch { ArrowDirection.Left => 180, ArrowDirection.Up => -90, ArrowDirection.Down => 90, _ => 0 });
        float x0 = -len / 2, y0 = -h / 2, shaftTop = y0 + h * 0.33f, shaftBottom = y0 + h * 0.66f, head = x0 + len * 0.5f;
        PointF[] arrow =
        [
            new(x0, shaftTop), new(head, shaftTop), new(head, y0), new(x0 + len, 0),
            new(head, y0 + h), new(head, shaftBottom), new(x0, shaftBottom),
        ];
        using (var fill = new SolidBrush(_isOn ? _onColor : _offColor))
            g.FillPolygon(fill, arrow);
        using (var edge = new Pen(Color.FromArgb(70, 70, 70)))
            g.DrawPolygon(edge, arrow);
        g.Restore(state);
    }
}
