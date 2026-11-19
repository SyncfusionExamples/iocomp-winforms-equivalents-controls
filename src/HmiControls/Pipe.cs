using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

public enum FlowDirection { Forward, Reverse }

/// <summary>Pipe segment with animated flow arrows (prototype for the iocomp Pipe).</summary>
public class Pipe : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private bool _flowing = true;
    private FlowDirection _direction = FlowDirection.Forward;
    private float _speed = 40;      // pixels per second
    private float _offset;
    private Color _pipeColor = Color.FromArgb(0, 128, 128);
    private Color _highlightColor = Color.FromArgb(124, 251, 251);
    private Color _arrowColor = Color.FromArgb(0, 128, 128);
    private DateTime _last = DateTime.UtcNow;

    public Pipe()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(260, 31);
        _timer.Tick += (_, _) => Advance();
        _timer.Start();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Flowing
    {
        get => _flowing;
        set { _flowing = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public FlowDirection Direction
    {
        get => _direction;
        set { _direction = value; Invalidate(); }
    }

    /// <summary>Arrow travel speed in pixels per second.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float Speed
    {
        get => _speed;
        set { _speed = Math.Max(0, value); }
    }

    /// <summary>Colour at the top and bottom edges of the pipe.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color PipeColor
    {
        get => _pipeColor;
        set { _pipeColor = value; Invalidate(); }
    }

    /// <summary>Colour along the centre line of the pipe (the cylindrical highlight).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color HighlightColor
    {
        get => _highlightColor;
        set { _highlightColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color ArrowColor
    {
        get => _arrowColor;
        set { _arrowColor = value; Invalidate(); }
    }

    private float Pitch => Math.Max(8f, Height * 0.68f);

    private void Advance()
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - _last).TotalSeconds;
        _last = now;
        if (!_flowing || _speed <= 0) return;
        var pitch = Pitch;
        _offset = (_offset + (_direction == FlowDirection.Forward ? 1 : -1) * _speed * dt) % pitch;
        if (_offset < 0) _offset += pitch;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var body = new RectangleF(0, 0, Width, Height);

        using (var fill = new LinearGradientBrush(body, _pipeColor, _highlightColor, LinearGradientMode.Vertical))
        {
            fill.InterpolationColors = new ColorBlend
            {
                Colors = [_pipeColor, _highlightColor, _pipeColor],
                Positions = [0f, 0.5f, 1f],
            };
            g.FillRectangle(fill, body);
        }

        if (!_flowing) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        var pitch = Pitch;
        var aw = Height * 0.3f;
        var ah = Height * 0.36f;
        var cy = Height / 2f;
        using var arrow = new SolidBrush(_arrowColor);
        for (var x = -pitch + _offset; x < Width + pitch; x += pitch)
        {
            PointF[] tri = _direction == FlowDirection.Forward
                ? [new(x, cy - ah / 2), new(x + aw, cy), new(x, cy + ah / 2)]
                : [new(x + aw, cy - ah / 2), new(x, cy), new(x + aw, cy + ah / 2)];
            g.FillPolygon(arrow, tri);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
