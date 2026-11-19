using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>
/// Pipe joint (prototype for the iocomp PipeJoint): a grey cylindrical section that joins pipe runs,
/// shaded across its width like iocomp's; set <see cref="Vertical"/> to shade along the other axis.
/// </summary>
public class PipeJoint : Control
{
    private Color _edgeColor = Color.FromArgb(128, 128, 128);
    private Color _highlightColor = Color.FromArgb(240, 240, 240);
    private bool _vertical = true;

    public PipeJoint()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(64, 128);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color EdgeColor { get => _edgeColor; set { _edgeColor = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color HighlightColor { get => _highlightColor; set { _highlightColor = value; Invalidate(); } }

    /// <summary>True: the joint runs vertically (bands are horizontal, as in the iocomp image).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Vertical { get => _vertical; set { _vertical = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        var r = ClientRectangle;
        if (r.Width < 2 || r.Height < 2) return;
        using var brush = new LinearGradientBrush(r, _edgeColor, _edgeColor, _vertical ? LinearGradientMode.Vertical : LinearGradientMode.Horizontal);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = [_edgeColor, _highlightColor, _edgeColor],
            Positions = [0f, 0.5f, 1f],
        };
        e.Graphics.FillRectangle(brush, r);
        // iocomp draws the joint as visible bands (a stepped gradient), so overlay faint band lines.
        using var band = new Pen(Color.FromArgb(40, 0, 0, 0));
        var n = Math.Max(4, (_vertical ? r.Height : r.Width) / 10);
        for (var i = 1; i < n; i++)
        {
            var p = i * (_vertical ? r.Height : r.Width) / n;
            if (_vertical) e.Graphics.DrawLine(band, 0, p, r.Width, p);
            else e.Graphics.DrawLine(band, p, 0, p, r.Height);
        }
        ControlPaint.DrawBorder(e.Graphics, r, Color.FromArgb(70, 70, 70), ButtonBorderStyle.Solid);
    }
}
