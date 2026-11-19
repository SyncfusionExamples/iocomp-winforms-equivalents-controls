using System.Drawing.Drawing2D;
using Syncfusion.Windows.Forms.Tools;

namespace PocApp.Cards;

// iocomp-style switches on top of Syncfusion controls:
//  - two-state switches: ToggleButton + a custom IToggleButtonRenderer (the documented extension point);
//    ToggleButton keeps the state, click/keyboard handling and ToggleStateChanged.
//  - three-state rocker and lever: CheckBoxAdv (CheckState / Tristate / CheckStateChanged) with OnPaint
//    overridden, because CheckBoxAdv has no renderer interface.
// "On" is ToggleState.Active / CheckState.Checked throughout.

internal static class SwitchDraw
{
    public static readonly Color Face = Color.FromArgb(240, 240, 240);

    /// <summary>Win32-style raised (or sunken) 3D border, 2 px, drawn inside <paramref name="r"/>.</summary>
    public static void Bevel(Graphics g, Rectangle r, bool raised = true)
    {
        Color outerLight = raised ? Color.FromArgb(247, 247, 247) : Color.FromArgb(160, 160, 160);
        Color innerLight = raised ? Color.White : Color.FromArgb(105, 105, 105);
        Color outerDark = raised ? Color.FromArgb(105, 105, 105) : Color.White;
        Color innerDark = raised ? Color.FromArgb(160, 160, 160) : Color.FromArgb(227, 227, 227);
        Edge(g, r, outerLight, outerDark);
        Edge(g, Rectangle.Inflate(r, -1, -1), innerLight, innerDark);
    }

    private static void Edge(Graphics g, Rectangle r, Color topLeft, Color bottomRight)
    {
        using var tl = new Pen(topLeft);
        using var br = new Pen(bottomRight);
        g.DrawLine(tl, r.Left, r.Bottom - 1, r.Left, r.Top);
        g.DrawLine(tl, r.Left, r.Top, r.Right - 1, r.Top);
        g.DrawLine(br, r.Right - 1, r.Top, r.Right - 1, r.Bottom - 1);
        g.DrawLine(br, r.Right - 1, r.Bottom - 1, r.Left, r.Bottom - 1);
    }

    /// <summary>Horizontal ridges (alternating light/dark lines) used on paddles and rocker faces.</summary>
    public static void Ridges(Graphics g, Rectangle r, Color light, Color dark, int pitch = 4)
    {
        using var lp = new Pen(light);
        using var dp = new Pen(dark);
        for (var y = r.Top + 1; y < r.Bottom - 1; y += pitch)
        {
            g.DrawLine(dp, r.Left, y, r.Right - 1, y);
            g.DrawLine(lp, r.Left, y + 1, r.Right - 1, y + 1);
        }
    }

    /// <summary>Largest rectangle with the given aspect ratio (width / height), centred in <paramref name="bounds"/>.</summary>
    public static Rectangle Fit(Rectangle bounds, float aspect)
    {
        var h = Math.Min(bounds.Height, (int)(bounds.Width / aspect));
        var w = (int)(h * aspect);
        return new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }
}

internal enum ToggleLook { Paddle, Led, Rocker }

/// <summary>Custom IToggleButtonRenderer: iocomp SwitchToggle (paddle), SwitchLed (caption + indicator) or SwitchRocker.</summary>
internal sealed class IocompToggleRenderer(ToggleLook look) : IToggleButtonRenderer
{
    public void DrawButton(PaintEventArgs e, ToggleButtonState toggleState, DisplayType displayMode, Font font,
        ActiveStateCollection activeState, InactiveStateCollection inactiveState, RightToLeft righttoLeft, bool isMouseHover, ToggleButton toggleButton)
    {
        var g = e.Graphics;
        var on = toggleState == ToggleButtonState.Active;
        var client = toggleButton.ClientRectangle;
        using (var back = new SolidBrush(toggleButton.Parent?.BackColor ?? SwitchDraw.Face))
            g.FillRectangle(back, client);

        switch (look)
        {
            case ToggleLook.Paddle: Paddle(g, SwitchDraw.Fit(client, 138f / 256), on); break;
            case ToggleLook.Led: Led(g, SwitchDraw.Fit(client, 1f), on, font, on ? activeState : null, on ? null : inactiveState); break;
            case ToggleLook.Rocker: Rocker(g, SwitchDraw.Fit(client, 138f / 256), on, on ? activeState.BackColor : inactiveState.BackColor); break;
        }

        if (toggleButton.Focused)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(client, -1, -1));
    }

    // Grey body; the ridged paddle sits in the top half when on and the bottom half when off.
    private static void Paddle(Graphics g, Rectangle r, bool on)
    {
        using (var body = new SolidBrush(Color.FromArgb(105, 105, 105)))
            g.FillRectangle(body, r);
        SwitchDraw.Bevel(g, r);
        var inner = Rectangle.Inflate(r, -4, -4);
        var half = inner.Height / 2;
        var paddle = new Rectangle(inner.X, on ? inner.Y : inner.Y + half, inner.Width, half);
        SwitchDraw.Ridges(g, paddle, Color.FromArgb(217, 217, 217), Color.FromArgb(52, 52, 52));
        using var divider = new Pen(Color.FromArgb(78, 78, 78));
        g.DrawLine(divider, inner.X, inner.Y + half, inner.Right - 1, inner.Y + half);
    }

    // Raised square face, large caption, coloured indicator bar along the bottom (iocomp SwitchLed).
    private static void Led(Graphics g, Rectangle r, bool on, Font font, ActiveStateCollection? active, InactiveStateCollection? inactive)
    {
        using (var face = new SolidBrush(SwitchDraw.Face))
            g.FillRectangle(face, r);
        SwitchDraw.Bevel(g, r);
        var text = active?.Text ?? inactive!.Text;
        var color = active?.BackColor ?? inactive!.BackColor;
        var bar = new Rectangle(r.X + r.Width / 13, r.Y + r.Height * 49 / 64, r.Width - 2 * (r.Width / 13), Math.Max(3, r.Height * 11 / 64));
        using (var indicator = new SolidBrush(color))
            g.FillRectangle(indicator, bar);
        SwitchDraw.Bevel(g, Rectangle.Inflate(bar, 1, 1), raised: false);

        using var big = new Font(font.FontFamily, Math.Max(8f, r.Height * 0.42f), FontStyle.Regular, GraphicsUnit.Pixel);
        var caption = new Rectangle(r.X, r.Y + r.Height / 16, r.Width, bar.Top - r.Y - r.Height / 16);
        TextRenderer.DrawText(g, text, big, caption, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    // Black outline, lamp window at the top (lit when on), ridged rocker face on the lower half.
    private static void Rocker(Graphics g, Rectangle r, bool on, Color lamp)
    {
        using (var face = new SolidBrush(SwitchDraw.Face))
            g.FillRectangle(face, r);
        using (var outline = new Pen(Color.Black))
            g.DrawRectangle(outline, r.X, r.Y, r.Width - 1, r.Height - 1);
        var inner = Rectangle.Inflate(r, -2, -2);
        SwitchDraw.Bevel(g, inner);
        var window = new Rectangle(inner.X + 2, inner.Y + 2, inner.Width - 4, inner.Height * 29 / 100);
        using (var lit = new SolidBrush(on ? lamp : GaugeDraw.Dim(lamp)))
            g.FillRectangle(lit, window);
        var ridged = new Rectangle(inner.X + 2, inner.Y + inner.Height / 2, inner.Width - 4, inner.Height / 2 - 2);
        SwitchDraw.Ridges(g, ridged, Color.FromArgb(251, 251, 251), Color.FromArgb(120, 120, 120), on ? 4 : 3);
    }

    public void DrawSlider(PaintEventArgs e, Point p, ToggleButtonState toggleState, SliderCollection slider, Font font,
        RightToLeft righttoLeft, bool isMouseMove, bool isMouseHover, ToggleButton toggleButton)
    {
        // The iocomp looks have no sliding thumb; the whole state is drawn in DrawButton.
    }
}

/// <summary>
/// CheckBoxAdv painted as the iocomp three-way rocker: up = Checked, centre = Indeterminate, down = Unchecked.
/// Like a real rocker, pressing a half moves one step toward it (down -> centre -> up, or the reverse);
/// Up/Down arrow keys do the same. CheckBoxAdv's own click cycling is turned off (AutoCheck = false).
/// </summary>
internal sealed class Rocker3WayBox : CheckBoxAdv
{
    public Rocker3WayBox()
    {
        Tristate = true;
        AutoCheck = false;
        Text = "";
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>Moves one position toward the top (up = true) or bottom half.</summary>
    public void Press(bool up) => CheckState = (CheckState, up) switch
    {
        (CheckState.Unchecked, true) => CheckState.Indeterminate,
        (CheckState.Indeterminate, true) => CheckState.Checked,
        (CheckState.Checked, false) => CheckState.Indeterminate,
        (CheckState.Indeterminate, false) => CheckState.Unchecked,
        _ => CheckState,
    };

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
        var r = SwitchDraw.Fit(ClientRectangle, 138f / 256);
        Press(up: e.Y < r.Top + r.Height / 2);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Up or Keys.Down) { Press(up: e.KeyCode == Keys.Up); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var back = new SolidBrush(Parent?.BackColor ?? SwitchDraw.Face))
            g.FillRectangle(back, ClientRectangle);
        var r = SwitchDraw.Fit(ClientRectangle, 138f / 256);
        using (var outline = new Pen(Color.Black))
            g.DrawRectangle(outline, r.X, r.Y, r.Width - 1, r.Height - 1);
        var inner = Rectangle.Inflate(r, -2, -2);
        var half = inner.Height / 2;
        var pressed = Color.FromArgb(180, 180, 180);
        var raised = Color.FromArgb(247, 247, 247);
        using (var top = new SolidBrush(CheckState == CheckState.Checked ? pressed : raised))
            g.FillRectangle(top, inner.X, inner.Y, inner.Width, half);
        using (var bottom = new SolidBrush(CheckState == CheckState.Unchecked ? pressed : raised))
            g.FillRectangle(bottom, inner.X, inner.Y + half, inner.Width, inner.Height - half);
        SwitchDraw.Bevel(g, new Rectangle(inner.X, inner.Y, inner.Width, half), CheckState != CheckState.Checked);
        SwitchDraw.Bevel(g, new Rectangle(inner.X, inner.Y + half, inner.Width, inner.Height - half), CheckState != CheckState.Unchecked);
        if (Focused) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(r, -4, -4));
    }
}

/// <summary>CheckBoxAdv painted as the iocomp lever switch: ball and shaft up when Checked, down when not.</summary>
internal sealed class LeverBox : CheckBoxAdv
{
    public LeverBox()
    {
        Text = "";
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var back = new SolidBrush(Parent?.BackColor ?? SwitchDraw.Face))
            g.FillRectangle(back, ClientRectangle);
        var r = SwitchDraw.Fit(ClientRectangle, 56f / 104);

        // Draw the "up" picture; flip it vertically for the "down" state.
        var state = g.Save();
        if (!Checked)
        {
            g.TranslateTransform(0, r.Top * 2 + r.Height);
            g.ScaleTransform(1, -1);
        }

        float W = r.Width, H = r.Height, x = r.X, y = r.Y;
        var plate = new RectangleF(x, y + H * 0.50f, W, H * 0.50f);
        using (var plateBrush = new SolidBrush(Color.FromArgb(247, 247, 247)))
            g.FillRectangle(plateBrush, plate);
        using (var lower = new SolidBrush(Color.FromArgb(180, 180, 180)))
            g.FillRectangle(lower, plate.X, plate.Bottom - H * 0.15f, plate.Width, H * 0.15f);

        // Shaft: trapezoid from the slot in the plate up to the ball.
        var shaft = new[]
        {
            new PointF(x + W * 0.30f, y + H * 0.30f), new PointF(x + W * 0.70f, y + H * 0.30f),
            new PointF(x + W * 0.86f, y + H * 0.52f), new PointF(x + W * 0.14f, y + H * 0.52f),
        };
        using (var shaftBrush = new LinearGradientBrush(new RectangleF(x, y + H * 0.29f, W, H * 0.24f), Color.FromArgb(206, 206, 206), Color.FromArgb(223, 223, 223), LinearGradientMode.Vertical))
            g.FillPolygon(shaftBrush, shaft);

        GaugeDraw.Sphere(g, new PointF(x + W / 2, y + W * 0.40f), W * 0.38f, Color.White, Color.FromArgb(185, 185, 185));
        g.Restore(state);

        if (Focused) ControlPaint.DrawFocusRectangle(g, r);
    }
}
