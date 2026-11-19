using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Draws an image (or, without one, the Text) rotated about its centre (prototype for the iocomp RotationDisplay).</summary>
public class RotationDisplay : Control
{
    private Image? _image;
    private float _angle;

    public RotationDisplay()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(160, 160);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Image? Image { get => _image; set { _image = value; Invalidate(); } }

    /// <summary>Rotation in degrees, clockwise.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float Angle { get => _angle; set { _angle = value % 360; Invalidate(); } }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TranslateTransform(Width / 2f, Height / 2f);
        g.RotateTransform(_angle);
        if (_image is not null)
        {
            // Fit so the rotated image's diagonal stays inside the control.
            var diag = MathF.Sqrt(_image.Width * _image.Width + _image.Height * _image.Height);
            var scale = Math.Min(Width, Height) / diag;
            var w = _image.Width * scale;
            var h = _image.Height * scale;
            g.DrawImage(_image, -w / 2, -h / 2, w, h);
            return;
        }
        using var brush = new SolidBrush(ForeColor);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(Text, Font, brush, PointF.Empty, sf);
    }
}
