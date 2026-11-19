using System.Drawing.Drawing2D;
using Syncfusion.Windows.Forms.Tools;

namespace PocApp.Cards;

/// <summary>
/// Syncfusion Clock renderer (documented extension point: Clock.Renderer) that draws iocomp-style tapered,
/// shaded hands and a thin second hand. Face, numerals and minute marks are still drawn by the base ClockRenderer;
/// the hand methods are not virtual there, so IClockRenderer is re-implemented for just those three.
/// </summary>
internal sealed class TaperedHandsClockRenderer : ClockRenderer, IClockRenderer
{
    void IClockRenderer.DrawHourHand(Graphics g, int length, float width, Point center, Color color, DateTime dateTime) =>
        Hand(g, center, length * 0.85f, Math.Max(4f, length * 0.12f), (dateTime.Hour % 12 + dateTime.Minute / 60f) * 30f, color);

    void IClockRenderer.DrawMinuteHand(Graphics g, int length, float width, Point center, Color color, DateTime dateTime) =>
        Hand(g, center, length, Math.Max(3f, length * 0.09f), (dateTime.Minute + dateTime.Second / 60f) * 6f, color);

    void IClockRenderer.DrawSecondHand(Graphics g, int length, float width, Point center, Color color, DateTime dateTime)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var tip = GaugeDraw.Polar(center, length, dateTime.Second * 6f - 90);
        using var pen = new Pen(color, 1f);
        g.DrawLine(pen, center, tip);
    }

    // iocomp face: 1-12 numerals instead of minute marks.
    void IClockRenderer.DrawMinutesLine(Graphics g, int radius, Point center, Color color, Font font, float thickness)
    {
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var numerals = new Font(font.FontFamily, Math.Max(7f, radius * 0.17f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        for (var h = 1; h <= 12; h++)
            g.DrawString(h.ToString(), numerals, brush, GaugeDraw.Polar(center, radius * 0.8f, h * 30f - 90), sf);
    }

    // Long narrow kite from a rounded hub to a point, shaded across its width like the iocomp hands.
    private static void Hand(Graphics g, PointF c, float length, float width, float clockDeg, Color color)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var a = clockDeg - 90;
        var tip = GaugeDraw.Polar(c, length, a);
        var left = GaugeDraw.Polar(c, width, a - 90);
        var right = GaugeDraw.Polar(c, width, a + 90);
        var tail = GaugeDraw.Polar(c, width * 0.8f, a + 180);
        using var path = new GraphicsPath();
        path.AddPolygon(new[] { tail, left, tip, right });
        using var brush = new LinearGradientBrush(left, right, Color.FromArgb(150, 150, 150), color);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = [Color.FromArgb(150, 150, 150), Color.White, color, Color.FromArgb(150, 150, 150)],
            Positions = [0f, 0.4f, 0.6f, 1f],
        };
        g.FillPath(brush, path);
        GaugeDraw.Sphere(g, c, width * 1.1f, Color.White, Color.FromArgb(170, 170, 170));
    }
}
