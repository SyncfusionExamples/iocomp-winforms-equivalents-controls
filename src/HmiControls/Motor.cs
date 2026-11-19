using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Motor symbol: base, legs and a tri-colour rotor that turns while <see cref="Running"/> (prototype for the iocomp Motor).</summary>
public class Motor : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private bool _running = true;
    private float _rpm = 30;
    private float _angle;
    private DateTime _last = DateTime.UtcNow;

    public Motor()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(160, 160);
        _timer.Tick += (_, _) => Advance();
        if (Animation.Enabled) _timer.Start();
    }

    public event EventHandler? RunningChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Running
    {
        get => _running;
        set
        {
            if (_running == value) return;
            _running = value;
            _last = DateTime.UtcNow;
            Invalidate();
            RunningChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Rotor speed in revolutions per minute.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float Rpm { get => _rpm; set => _rpm = Math.Max(0, value); }

    private void Advance()
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - _last).TotalSeconds;
        _last = now;
        if (!_running || _rpm <= 0) return;
        _angle = (_angle + _rpm * 6f * dt) % 360f;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var s = Math.Min(Width, Height);
        if (s < 30) return;
        var ox = (Width - s) / 2f;
        var oy = (Height - s) / 2f;
        RectangleF R(float x, float y, float w, float h) => new(ox + x * s, oy + y * s, w * s, h * s);

        // Shaft housings left and right, then legs to the feet.
        using (var housing = new LinearGradientBrush(R(0, 0.35f, 1, 0.3f), Color.White, Color.FromArgb(190, 190, 190), LinearGradientMode.Vertical))
        {
            g.FillRectangle(housing, R(0, 0.35f, 0.3f, 0.3f));
            g.FillRectangle(housing, R(0.7f, 0.35f, 0.3f, 0.3f));
        }
        using (var legPen = new Pen(Color.FromArgb(200, 200, 200), s * 0.05f))
        {
            g.DrawLine(legPen, ox + 0.3f * s, oy + 0.72f * s, ox + 0.12f * s, oy + 0.96f * s);
            g.DrawLine(legPen, ox + 0.7f * s, oy + 0.72f * s, ox + 0.88f * s, oy + 0.96f * s);
        }
        using (var feet = new SolidBrush(Color.FromArgb(225, 225, 225)))
        {
            g.FillRectangle(feet, R(0, 0.95f, 0.2f, 0.05f));
            g.FillRectangle(feet, R(0.8f, 0.95f, 0.2f, 0.05f));
        }

        // Rim and tri-colour rotor (blue / yellow / red, two sectors each).
        var rim = R(0.2f, 0.2f, 0.6f, 0.6f);
        using (var rimBrush = new LinearGradientBrush(rim, Color.White, Color.FromArgb(185, 185, 185), 45f))
            g.FillEllipse(rimBrush, rim);
        var rotor = RectangleF.Inflate(rim, -s * 0.045f, -s * 0.045f);
        Color[] colors = [Color.Blue, Color.Yellow, Color.Red];
        for (var i = 0; i < 6; i++)
        {
            using var sector = new SolidBrush(colors[i % 3]);
            g.FillPie(sector, rotor.X, rotor.Y, rotor.Width, rotor.Height, -120 + _angle + i * 60, 60);
        }
        if (!_running)
        {
            using var dim = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
            g.FillEllipse(dim, rotor);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
