using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Rolling-drum digit display (prototype for the iocomp Odometer). Digits roll toward <see cref="Value"/>.</summary>
public class Odometer : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private double _value;
    private double _displayed;
    private int _digitCount = 6;

    public Odometer()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        Size = new Size(240, 48);
        _timer.Tick += (_, _) => Step();
    }

    /// <summary>Raised when the displayed (animated) value reaches <see cref="Value"/>.</summary>
    public event EventHandler? RollCompleted;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int DigitCount
    {
        get => _digitCount;
        set { _digitCount = Math.Clamp(value, 1, 12); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowLeadingZeros { get; set; }

    /// <summary>Target value. Negative values are clamped to 0.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Max(0, value);
            if (!Animation.Enabled) { _displayed = _value; Invalidate(); }
            else if (Math.Abs(_value - _displayed) > 1e-9) _timer.Start();
        }
    }

    /// <summary>The value currently drawn while the drum rolls.</summary>
    public double DisplayedValue => _displayed;

    private void Step()
    {
        var delta = _value - _displayed;
        var step = delta * 0.2;
        if (Math.Abs(step) < 0.02) step = Math.Sign(delta) * 0.02;
        if (Math.Abs(delta) <= Math.Abs(step))
        {
            _displayed = _value;
            _timer.Stop();
            Invalidate();
            RollCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }
        _displayed += step;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float cw = (float)Width / _digitCount;
        float ch = Height;
        using var font = new Font(Font.FontFamily, Math.Max(6f, ch * 0.55f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var darkText = new SolidBrush(Color.Black);
        using var lightText = new SolidBrush(Color.White);

        // iocomp look: one continuous, seamless light drum; the units drum is darker with white digits.
        var full = new RectangleF(0, 0, Width, Height);
        using (var drum = new LinearGradientBrush(full, Color.Gray, Color.Gray, LinearGradientMode.Vertical))
        {
            drum.InterpolationColors = new ColorBlend
            {
                Colors = [Color.FromArgb(204, 204, 204), Color.White, Color.FromArgb(204, 204, 204)],
                Positions = [0f, 0.5f, 1f],
            };
            g.FillRectangle(drum, full);
        }

        for (var k = 0; k < _digitCount; k++)
        {
            var p = _digitCount - 1 - k;
            var cell = new RectangleF(k * cw, 0, cw, ch);

            if (p == 0)
            {
                using var units = new LinearGradientBrush(cell, Color.Gray, Color.Gray, LinearGradientMode.Vertical);
                units.InterpolationColors = new ColorBlend
                {
                    Colors = [Color.FromArgb(197, 197, 197), Color.FromArgb(147, 147, 147), Color.FromArgb(129, 129, 129),
                              Color.FromArgb(162, 162, 162), Color.FromArgb(197, 197, 197)],
                    Positions = [0f, 0.33f, 0.5f, 0.75f, 1f],
                };
                g.FillRectangle(units, cell);
            }

            var scroll = _displayed / Math.Pow(10, p);
            var whole = (long)Math.Floor(scroll);
            var frac = scroll - whole;
            // the units drum rolls continuously; higher drums roll only as the lower ones wrap past 9
            var roll = p == 0 ? frac : Math.Clamp((frac - 0.8) / 0.2, 0, 1);
            var digit = (int)(whole % 10);
            var current = !ShowLeadingZeros && whole == 0 && p > 0 ? "" : digit.ToString();
            var next = ((digit + 1) % 10).ToString();
            var textBrush = p == 0 ? lightText : darkText;

            g.SetClip(cell);
            g.DrawString(current, font, textBrush, new RectangleF(cell.X, (float)(-roll * ch), cw, ch), sf);
            if (roll > 0)
                g.DrawString(next, font, textBrush, new RectangleF(cell.X, (float)((1 - roll) * ch), cw, ch), sf);
            g.ResetClip();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
