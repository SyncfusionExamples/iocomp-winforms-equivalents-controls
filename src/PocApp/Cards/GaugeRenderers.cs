using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Syncfusion.Windows.Forms.Gauge;
using GRange = Syncfusion.Windows.Forms.Gauge.Range;

namespace PocApp.Cards;

// iocomp-style painting for the Syncfusion gauges, through the documented extension point
// (IRadialGaugeRenderer / ILinearGaugeRenderer assigned to the gauge's Renderer property).
// The gauge still owns Value, Minimum/Maximum, MajorDifference, Ranges, colours and mouse input;
// a renderer only paints, with all geometry computed from the client size. Everything is drawn in
// UpdateRenderer, so the per-part callbacks of the interfaces are left empty.

internal static class GaugeDraw
{
    public static PointF Polar(PointF c, float r, float deg)
    {
        var a = deg * MathF.PI / 180f;
        return new PointF(c.X + r * MathF.Cos(a), c.Y + r * MathF.Sin(a));
    }

    public static Color Dim(Color c) => Color.FromArgb(c.R / 2, c.G / 2, c.B / 2);

    public static Color Mix(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    public static Font PixelFont(Font baseFont, float px) => new(baseFont.FontFamily, Math.Max(6f, px), FontStyle.Regular, GraphicsUnit.Pixel);

    public static string Label(float v) => v.ToString("0.0");

    public static Color ColorAt(RadialGauge gauge, float v)
    {
        foreach (GRange r in gauge.Ranges)
            if (v >= r.StartValue && v <= r.EndValue) return r.Color;
        return gauge.NeedleColor;
    }

    public static Color ColorAt(LinearGauge gauge, float v)
    {
        foreach (LinearRange r in gauge.Ranges)
            if (v >= r.StartValue && v <= r.EndValue) return r.Color;
        return gauge.ValueIndicatorColor;
    }

    /// <summary>Glossy sphere with the highlight up and to the left (iocomp knob, compass centre, thermometer bulb).</summary>
    public static void Sphere(Graphics g, PointF c, float r, Color light, Color edge)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(c.X - r, c.Y - r, 2 * r, 2 * r);
        using var brush = new PathGradientBrush(path)
        {
            CenterPoint = new PointF(c.X - r * 0.4f, c.Y - r * 0.4f),
            CenterColor = light,
            SurroundColors = [edge],
        };
        g.FillPath(brush, path);
    }

    /// <summary>Draws text so that its box just touches radius <paramref name="r"/> on the side facing the centre.</summary>
    public static void TextOutside(Graphics g, string text, Font font, Brush brush, PointF c, float r, float deg)
    {
        var size = g.MeasureString(text, font);
        var a = deg * MathF.PI / 180f;
        var push = MathF.Abs(MathF.Cos(a)) * size.Width / 2 + MathF.Abs(MathF.Sin(a)) * size.Height / 2;
        var p = Polar(c, r + push, deg);
        g.DrawString(text, font, brush, p.X - size.Width / 2, p.Y - size.Height / 2);
    }

    /// <summary>Rectangle with a semicircular bottom (tube glass and liquid column).</summary>
    public static GraphicsPath RoundBottom(RectangleF r)
    {
        var p = new GraphicsPath();
        var d = r.Width;
        p.AddLine(r.Left, r.Top, r.Left, r.Bottom - d / 2);
        p.AddArc(r.Left, r.Bottom - d, d, d, 180, -180);
        p.AddLine(r.Right, r.Bottom - d / 2, r.Right, r.Top);
        p.CloseFigure();
        return p;
    }

    /// <summary>Horizontal cylinder shading: darker edges, highlight left of centre.</summary>
    public static LinearGradientBrush Cylinder(RectangleF r, Color edge, Color highlight, Color mid)
    {
        var brush = new LinearGradientBrush(new RectangleF(r.X - 1, r.Y, r.Width + 2, Math.Max(1, r.Height)), edge, edge, LinearGradientMode.Horizontal);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = [edge, highlight, mid, edge],
            Positions = [0f, 0.35f, 0.7f, 1f],
        };
        return brush;
    }
}

internal abstract class RadialRenderer(RadialGauge gauge) : IRadialGaugeRenderer
{
    protected RadialGauge Gauge { get; } = gauge;

    protected float AngleOf(float v) =>
        Gauge.StartAngle + (v - Gauge.MinimumValue) / (Gauge.MaximumValue - Gauge.MinimumValue) * Gauge.SweepAngle;

    public void UpdateRenderer(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var b = Gauge.ClientRectangle;
        var size = Math.Min(b.Width, b.Height);
        if (size < 40) return;
        Paint(g, new PointF(b.Width / 2f, b.Height / 2f), size);
    }

    protected abstract void Paint(Graphics g, PointF c, float s);

    void IRadialGaugeRenderer.AdjustFontBounds(Graphics graphics, Font font) { }
    void IRadialGaugeRenderer.DrawOuterArc(Graphics e, int arcStart, int arcEnd, Point center, int radius) { }
    void IRadialGaugeRenderer.DrawMultipleNeedle(Graphics graphics, Needle needle, int arcStart, int arcEnd, int needleRadius, int needleWidth, Point center) { }
    void IRadialGaugeRenderer.DrawNeedle(Graphics graphics, int arcStart, int arcEnd, int needleRadius, int needleWidth, Point center) { }
    void IRadialGaugeRenderer.DrawGaugeLabel(Graphics graphics, Point center, int radius) { }
    void IRadialGaugeRenderer.DrawRanges(Graphics graphics, GraphicsPath path, int arcStart, int arcEnd, int arcRadius, Point center) { }
    void IRadialGaugeRenderer.DrawTickMarks(Graphics graphics, GraphicsPath path, int arcRadius, int arcStart, int arcEnd, int majorTickWidth, Point center, int labelRadius) { }
    void IRadialGaugeRenderer.DrawCircle(Graphics graphics, Point center, int radius, int startAngle, int sweepAngle, float frameThickness, Color arcColor, Color fillColor) { }
}

/// <summary>iocomp Knob: glossy sphere with a red position dot, ticks and "0.0" labels around it.</summary>
internal sealed class KnobRenderer(RadialGauge gauge) : RadialRenderer(gauge)
{
    private const int MinorPerMajor = 5;

    protected override void Paint(Graphics g, PointF c, float s)
    {
        var rTick = s * 0.38f;
        using var font = GaugeDraw.PixelFont(Gauge.Font, s * 0.045f);
        using var major = new Pen(Gauge.MajorTickMarkColor, Math.Max(1f, s * 0.006f));
        using var minor = new Pen(Gauge.MinorTickMarkColor, 1f);
        using var text = new SolidBrush(Gauge.ScaleLabelColor);

        var step = Gauge.MajorDifference;
        var count = (int)Math.Round((Gauge.MaximumValue - Gauge.MinimumValue) / step);
        for (var i = 0; i <= count; i++)
        {
            var v = Gauge.MinimumValue + i * step;
            var a = AngleOf(v);
            g.DrawLine(major, GaugeDraw.Polar(c, rTick - s * 0.05f, a), GaugeDraw.Polar(c, rTick, a));
            GaugeDraw.TextOutside(g, GaugeDraw.Label(v), font, text, c, rTick + s * 0.02f, a);
            if (i == count) break;
            for (var k = 1; k < MinorPerMajor; k++)
            {
                var am = AngleOf(v + k * step / MinorPerMajor);
                g.DrawLine(minor, GaugeDraw.Polar(c, rTick - s * 0.02f, am), GaugeDraw.Polar(c, rTick, am));
            }
        }

        var rKnob = s * 0.31f;
        GaugeDraw.Sphere(g, c, rKnob, Color.White, Color.FromArgb(196, 196, 196));

        var dot = GaugeDraw.Polar(c, rKnob * 0.84f, AngleOf(Gauge.Value));
        var rDot = s * 0.017f;
        GaugeDraw.Sphere(g, dot, rDot, GaugeDraw.Mix(Gauge.NeedleColor, Color.White, 0.5f), Gauge.NeedleColor);
    }
}

/// <summary>iocomp Compass: navy ring with N/NE/E... labels, glossy centre with the heading, yellow pointer.</summary>
internal sealed class CompassRenderer(RadialGauge gauge) : RadialRenderer(gauge)
{
    private static readonly string[] Points = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];

    protected override void Paint(Graphics g, PointF c, float s)
    {
        var rOuter = s * 0.49f;
        using (var rim = new LinearGradientBrush(new RectangleF(c.X - rOuter, c.Y - rOuter, 2 * rOuter, 2 * rOuter),
                   Color.FromArgb(235, 235, 235), Color.FromArgb(140, 140, 140), 45f))
            g.FillEllipse(rim, c.X - rOuter, c.Y - rOuter, 2 * rOuter, 2 * rOuter);
        var rRing = rOuter - s * 0.022f;
        using (var ring = new SolidBrush(Gauge.BackgroundGradientStartColor))
            g.FillEllipse(ring, c.X - rRing, c.Y - rRing, 2 * rRing, 2 * rRing);

        var rCentre = s * 0.30f;
        using var font = GaugeDraw.PixelFont(Gauge.Font, s * 0.055f);
        using var tick = new Pen(Gauge.MajorTickMarkColor, Math.Max(1.5f, s * 0.008f));
        using var text = new SolidBrush(Gauge.ScaleLabelColor);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (var i = 0; i < Points.Length; i++)
        {
            var a = Gauge.StartAngle + i * 45f;
            g.DrawLine(tick, GaugeDraw.Polar(c, rCentre + s * 0.035f, a), GaugeDraw.Polar(c, rCentre + s * 0.06f, a));
            var p = GaugeDraw.Polar(c, s * 0.405f, a);
            g.DrawString(Points[i], font, text, p, sf);
        }

        GaugeDraw.Sphere(g, c, rCentre, Color.White, Color.FromArgb(190, 190, 190));

        // Pointer: small triangle between the centre and the ring labels, tip outward at the heading.
        var heading = AngleOf(Gauge.Value);
        var baseCentre = GaugeDraw.Polar(c, rCentre + 1, heading);
        var tip = GaugeDraw.Polar(c, rCentre + s * 0.065f, heading);
        var side = GaugeDraw.Polar(new PointF(0, 0), s * 0.025f, heading + 90);
        using (var pointer = new SolidBrush(Gauge.NeedleColor))
            g.FillPolygon(pointer, new[] { tip, new PointF(baseCentre.X + side.X, baseCentre.Y + side.Y), new PointF(baseCentre.X - side.X, baseCentre.Y - side.Y) });

        using var valueFont = GaugeDraw.PixelFont(Gauge.Font, s * 0.05f);
        using var valueBrush = new SolidBrush(Gauge.GaugeValueColor);
        g.DrawString(Gauge.Value.ToString("0.0") + "\u00B0", valueFont, valueBrush, c, sf);
    }
}

/// <summary>iocomp LedSpiral: ring of LED segments; lit up to Value, unlit ones drawn at half brightness.</summary>
internal sealed class LedSpiralRenderer(RadialGauge gauge) : RadialRenderer(gauge)
{
    private const int Segments = 50;

    protected override void Paint(Graphics g, PointF c, float s)
    {
        var rOut = s * 0.49f;
        var rIn = s * 0.39f;
        var step = Gauge.SweepAngle / (float)Segments;
        var half = step * 0.28f;
        for (var i = 0; i < Segments; i++)
        {
            var v = Gauge.MinimumValue + (i + 0.5f) / Segments * (Gauge.MaximumValue - Gauge.MinimumValue);
            var a = Gauge.StartAngle + (i + 0.5f) * step;
            var color = GaugeDraw.ColorAt(Gauge, v);
            using var brush = new SolidBrush(v <= Gauge.Value ? color : GaugeDraw.Dim(color));
            g.FillPolygon(brush, new[]
            {
                GaugeDraw.Polar(c, rIn, a - half), GaugeDraw.Polar(c, rOut, a - half),
                GaugeDraw.Polar(c, rOut, a + half), GaugeDraw.Polar(c, rIn, a + half),
            });
        }
    }
}

internal abstract class LinearRenderer(LinearGauge gauge) : ILinearGaugeRenderer
{
    protected LinearGauge Gauge { get; } = gauge;

    protected float Fraction(float v) =>
        Math.Clamp((v - Gauge.MinimumValue) / (Gauge.MaximumValue - Gauge.MinimumValue), 0f, 1f);

    public void UpdateRenderer(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var b = Gauge.ClientRectangle;
        if (b.Width < 20 || b.Height < 60) return;
        Paint(g, b);
    }

    protected abstract void Paint(Graphics g, Rectangle b);

    /// <summary>Vertical scale: axis line at <paramref name="x"/>, ticks to the right, "0.0" labels after them.</summary>
    protected void VerticalScale(Graphics g, float x, float yMin, float yMax, float tick, Font font)
    {
        using var major = new Pen(Gauge.MajorTickMarkColor);
        using var minor = new Pen(Gauge.MinorTickMarkColor);
        using var text = new SolidBrush(Gauge.ScaleLabelColor);
        g.DrawLine(major, x, yMax, x, yMin);
        var step = Gauge.MajorDifference;
        var count = (int)Math.Round((Gauge.MaximumValue - Gauge.MinimumValue) / step);
        var h = font.GetHeight(g);
        for (var i = 0; i <= count; i++)
        {
            var v = Gauge.MinimumValue + i * step;
            var y = yMin - Fraction(v) * (yMin - yMax);
            g.DrawLine(major, x, y, x + tick, y);
            g.DrawString(GaugeDraw.Label(v), font, text, x + tick + 2, y - h / 2);
            if (i == count) break;
            for (var k = 1; k <= Gauge.MinorTickCount; k++)
            {
                var ym = yMin - Fraction(v + k * (float)step / (Gauge.MinorTickCount + 1)) * (yMin - yMax);
                g.DrawLine(minor, x, ym, x + tick / 2, ym);
            }
        }
    }

    void ILinearGaugeRenderer.DrawFrame(Graphics graphics) { }
    void ILinearGaugeRenderer.DrawLines(Graphics graphics) { }
    void ILinearGaugeRenderer.DrawRanges(Graphics graphics) { }
    void ILinearGaugeRenderer.DrawPointer(Graphics graphics) { }
}

/// <summary>iocomp GaugeTube (glass tube with cap) or Thermometer (thin tube with bulb), scale on the right.</summary>
internal sealed class TubeRenderer(LinearGauge gauge, bool bulb) : LinearRenderer(gauge)
{
    protected override void Paint(Graphics g, Rectangle b)
    {
        using var font = GaugeDraw.PixelFont(Gauge.Font, Math.Clamp(b.Height * 0.055f, 7f, 13f));
        var fh = font.GetHeight(g);
        var labelW = g.MeasureString(GaugeDraw.Label(Gauge.MaximumValue), font).Width;
        var tw = bulb ? Math.Clamp(b.Height * 0.06f, 6f, 20f) : Math.Clamp(b.Height * 0.15f, 12f, 44f);
        var bulbR = bulb ? tw * 1.15f : 0f;
        var tick = Math.Max(4f, b.Height * 0.03f);
        var bodyW = Math.Max(tw, 2 * bulbR);
        var gap = tw * 0.3f + 3;
        var total = bodyW + gap + tick + 2 + labelW;
        var x0 = b.X + (b.Width - total) / 2f;
        var cx = x0 + bodyW / 2f;

        var yMax = b.Y + Math.Max(fh / 2, tw * 0.4f) + 2;                      // Maximum on the scale
        var yMin = bulb ? b.Bottom - 2 - bulbR * 1.9f                           // Minimum on the scale
                        : b.Bottom - 2 - Math.Max(fh / 2, tw / 2);
        var glassTop = yMax - tw * 0.25f;
        var glassBottom = bulb ? yMin + bulbR : yMin + tw / 2;
        var level = yMin - Fraction(Gauge.Value) * (yMin - yMax);
        var liquid = Gauge.ValueIndicatorColor;

        var glass = new RectangleF(cx - tw / 2, glassTop, tw, glassBottom - glassTop);
        using (var path = GaugeDraw.RoundBottom(glass))
        using (var brush = GaugeDraw.Cylinder(glass, Color.FromArgb(175, 175, 175), Color.FromArgb(252, 252, 252), Color.FromArgb(228, 228, 228)))
            g.FillPath(brush, path);

        var inset = bulb ? tw * 0.2f : tw * 0.1f;
        var column = new RectangleF(glass.X + inset, level, glass.Width - 2 * inset, Math.Max(0, glass.Bottom - inset - level));
        if (column.Height > column.Width / 2)
        {
            using var path = GaugeDraw.RoundBottom(column);
            using var brush = GaugeDraw.Cylinder(column, GaugeDraw.Mix(liquid, Color.Black, 0.25f), GaugeDraw.Mix(liquid, Color.White, 0.4f), liquid);
            g.FillPath(brush, path);
        }

        if (bulb)
        {
            GaugeDraw.Sphere(g, new PointF(cx, yMin + bulbR * 0.9f), bulbR, GaugeDraw.Mix(liquid, Color.White, 0.6f), GaugeDraw.Mix(liquid, Color.Black, 0.2f));
        }
        else
        {
            // Rim of the open top.
            var cap = new RectangleF(glass.X - tw * 0.04f, glassTop - tw * 0.16f, glass.Width + tw * 0.08f, tw * 0.32f);
            using var rim = new LinearGradientBrush(cap, Color.FromArgb(240, 240, 240), Color.FromArgb(150, 150, 150), LinearGradientMode.Vertical);
            g.FillEllipse(rim, cap);
            using var hole = new SolidBrush(Color.FromArgb(215, 215, 215));
            g.FillEllipse(hole, RectangleF.Inflate(cap, -tw * 0.08f, -tw * 0.06f));
        }

        VerticalScale(g, x0 + bodyW + gap, yMin, yMax, tick, font);
    }
}

/// <summary>iocomp Tank stand-in: silver capsule vessel, black interior, blue liquid up to Value.</summary>
internal sealed class TankRenderer(LinearGauge gauge) : LinearRenderer(gauge)
{
    protected override void Paint(Graphics g, Rectangle b)
    {
        var h = b.Height - 4f;
        var w = Math.Min(b.Width - 4f, h * 0.53f);
        var shell = new RectangleF(b.X + (b.Width - w) / 2f, b.Y + 2, w, h);
        using (var path = Capsule(shell))
        using (var brush = GaugeDraw.Cylinder(shell, Color.FromArgb(181, 181, 181), Color.FromArgb(234, 234, 234), Color.FromArgb(206, 206, 206)))
            g.FillPath(brush, path);

        var inner = new RectangleF(shell.X + w * 0.16f, shell.Y + w * 0.45f, w * 0.68f, h - w * 0.9f);
        using (var black = new SolidBrush(Color.Black))
            g.FillRectangle(black, inner);
        var level = inner.Height * Fraction(Gauge.Value);
        using (var liquid = new SolidBrush(Gauge.ValueIndicatorColor))
            g.FillRectangle(liquid, inner.X, inner.Bottom - level, inner.Width, level);
    }

    private static GraphicsPath Capsule(RectangleF r)
    {
        var p = new GraphicsPath();
        var d = r.Width;
        p.AddArc(r.X, r.Y, d, d, 180, 180);
        p.AddArc(r.X, r.Bottom - d, d, d, 0, 180);
        p.CloseFigure();
        return p;
    }
}

/// <summary>
/// iocomp SlidingScale / SlidingCompass: a vertical tape that scrolls so Value sits on the blue centre line,
/// with yellow markers on both sides. <paramref name="span"/> is the value range visible on the tape;
/// <paramref name="wrap"/> makes labels wrap at 360 (compass).
/// </summary>
internal sealed class SlidingTapeRenderer(LinearGauge gauge, float span, bool wrap) : LinearRenderer(gauge)
{
    protected override void Paint(Graphics g, Rectangle b)
    {
        // Keep iocomp's narrow tape proportions (93 x 256) instead of stretching to the slot.
        var w = Math.Min(b.Width, (int)(b.Height * 93f / 256));
        b = new Rectangle(b.X + (b.Width - w) / 2, b.Y, w, b.Height);
        var marker = Math.Max(6f, b.Width * 0.09f);
        var tape = new RectangleF(b.X + marker + 2, b.Y, b.Width - 2 * marker - 4, b.Height);
        var cy = tape.Y + tape.Height / 2f;

        using (var shade = new LinearGradientBrush(tape, Color.Black, Color.Black, LinearGradientMode.Vertical))
        {
            shade.InterpolationColors = new ColorBlend
            {
                Colors = [Color.FromArgb(45, 45, 45), Color.FromArgb(250, 250, 250), Color.FromArgb(51, 51, 51)],
                Positions = [0f, 0.5f, 1f],
            };
            g.FillRectangle(shade, tape);
        }

        using var font = GaugeDraw.PixelFont(Gauge.Font, Math.Clamp(b.Height * 0.065f, 9f, 14f));
        using var pen = new Pen(Gauge.MajorTickMarkColor);
        using var text = new SolidBrush(Gauge.ScaleLabelColor);
        var fh = font.GetHeight(g);
        var perUnit = tape.Height / span;
        var majorStep = (float)Gauge.MajorDifference;
        var minorStep = majorStep / (Gauge.MinorTickCount + 1);
        var v = Gauge.Value;

        g.SetClip(tape);
        var first = MathF.Floor((v - span / 2) / minorStep) * minorStep;
        for (var u = first; u <= v + span / 2 + minorStep; u += minorStep)
        {
            if (!wrap && (u < Gauge.MinimumValue - 0.001f || u > Gauge.MaximumValue + 0.001f)) continue;
            var y = cy - (u - v) * perUnit;
            var isMajor = MathF.Abs(MathF.IEEERemainder(u, majorStep)) < minorStep / 4;
            var len = isMajor ? tape.Width * 0.12f : tape.Width * 0.06f;
            g.DrawLine(pen, tape.X, y, tape.X + len, y);
            if (!isMajor) continue;
            var label = wrap ? (MathF.Round(u) % 360 + 360) % 360 : u;
            g.DrawString(GaugeDraw.Label(label), font, text, tape.X + tape.Width * 0.16f, y - fh / 2);
        }
        g.ResetClip();

        using (var line = new Pen(Gauge.ValueIndicatorColor, 2f))
            g.DrawLine(line, tape.X, cy, tape.Right, cy);
        using var yellow = new SolidBrush(Gauge.NeedleColor);
        var half = marker * 0.55f;
        g.FillPolygon(yellow, new[] { new PointF(b.X, cy - half), new PointF(b.X + marker, cy), new PointF(b.X, cy + half) });
        g.FillPolygon(yellow, new[] { new PointF(b.Right, cy - half), new PointF(b.Right - marker, cy), new PointF(b.Right, cy + half) });
    }
}

/// <summary>iocomp LedBar: narrow black bar of thin LED segments; lit up to Value, unlit at half brightness.</summary>
internal sealed class LedBarRenderer(LinearGauge gauge) : LinearRenderer(gauge)
{
    private const float Pitch = 3f;

    protected override void Paint(Graphics g, Rectangle b)
    {
        g.SmoothingMode = SmoothingMode.None;
        var h = b.Height - 4f;
        var w = Math.Clamp(h * 0.085f, 10f, 22f);
        var bar = new RectangleF(b.X + (b.Width - w) / 2f, b.Y + 2, w, h);
        using (var back = new SolidBrush(Gauge.BackgroundGradientStartColor))
            g.FillRectangle(back, bar);
        using (var edge = new Pen(Color.FromArgb(60, 60, 60)))
            g.DrawRectangle(edge, bar.X, bar.Y, bar.Width, bar.Height);

        var n = (int)((h - 2) / Pitch);
        for (var i = 0; i < n; i++)
        {
            var v = Gauge.MinimumValue + (i + 0.5f) / n * (Gauge.MaximumValue - Gauge.MinimumValue);
            var color = GaugeDraw.ColorAt(Gauge, v);
            using var brush = new SolidBrush(v <= Gauge.Value ? color : GaugeDraw.Dim(color));
            g.FillRectangle(brush, bar.X + 2, bar.Bottom - 1 - (i + 1) * Pitch, bar.Width - 3, Pitch - 1);
        }
    }
}
