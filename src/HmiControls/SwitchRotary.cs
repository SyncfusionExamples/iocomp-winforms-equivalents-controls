using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HmiControls;

/// <summary>Multi-position rotary selector with a pointer bar and leader-line labels (prototype for iocomp SwitchRotary).</summary>
public class SwitchRotary : Control
{
    private string[] _items = ["Low", "Medium", "High"];
    private int _selectedIndex = 1;
    private float _startAngle = 180;
    private float _sweepAngle = 90;
    private PointF _pivot;

    public SwitchRotary()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        Size = new Size(256, 190);
    }

    public event EventHandler? SelectedIndexChanged;

    /// <summary>Position labels, placed evenly from <see cref="StartAngle"/> across <see cref="SweepAngle"/>.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string[] Items
    {
        get => _items;
        set
        {
            _items = value is { Length: > 0 } ? value : ["Off"];
            _selectedIndex = Math.Clamp(_selectedIndex, 0, _items.Length - 1);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var v = Math.Clamp(value, 0, _items.Length - 1);
            if (v == _selectedIndex) return;
            _selectedIndex = v;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string SelectedItem => _items[_selectedIndex];

    /// <summary>Angle of the first position in degrees, clockwise from 3 o'clock.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float StartAngle
    {
        get => _startAngle;
        set { _startAngle = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float SweepAngle
    {
        get => _sweepAngle;
        set { _sweepAngle = value; Invalidate(); }
    }

    private float AngleOf(int i) => _items.Length < 2 ? _startAngle : _startAngle + _sweepAngle * i / (_items.Length - 1);

    /// <summary>Selects the position whose angle is closest to the direction of <paramref name="p"/> from the pivot.</summary>
    public void SelectAt(Point p)
    {
        var angle = Math.Atan2(p.Y - _pivot.Y, p.X - _pivot.X) * 180 / Math.PI;
        var best = 0;
        var bestDiff = double.MaxValue;
        for (var i = 0; i < _items.Length; i++)
        {
            var diff = Math.Abs(((angle - AngleOf(i) + 540) % 360) - 180);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        SelectedIndex = best;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Left) SelectAt(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button == MouseButtons.Left) SelectAt(e.Location);
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Left or Keys.Down) SelectedIndex--;
        else if (e.KeyCode is Keys.Right or Keys.Up) SelectedIndex++;
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    private readonly record struct LabelLayout(PointF Dot, PointF Elbow, PointF End, RectangleF Text, StringAlignment Align);

    // Layout relative to a pivot at (0,0); the caller centres the result in the control.
    private LabelLayout[] LayoutLabels(Graphics g, float size, Font normal, Font big, out RectangleF bounds)
    {
        var r = size * 0.34f;
        var labels = new LabelLayout[_items.Length];
        var sel = AngleOf(_selectedIndex) * Math.PI / 180;
        var tail = new PointF((float)-Math.Cos(sel) * r * 1.15f, (float)-Math.Sin(sel) * r * 1.15f);
        bounds = RectangleF.FromLTRB(Math.Min(0, tail.X), Math.Min(0, tail.Y), Math.Max(0, tail.X), Math.Max(0, tail.Y));

        for (var i = 0; i < _items.Length; i++)
        {
            var a = AngleOf(i) * Math.PI / 180;
            var cos = (float)Math.Cos(a);
            var sin = (float)Math.Sin(a);
            var dot = new PointF(r * cos, r * sin);
            var font = i == _selectedIndex ? big : normal;
            var ts = g.MeasureString(_items[i], font);
            PointF elbow, end;
            RectangleF text;
            StringAlignment align;

            if (Math.Abs(cos) < 0.35f)
            {
                // Near vertical: a radial leader with the label centred beyond it (iocomp "High").
                elbow = end = new PointF(dot.X, dot.Y + sin * size * 0.09f);
                var y = sin < 0 ? end.Y - 2 - ts.Height : end.Y + 2;
                text = new RectangleF(end.X - ts.Width / 2, y, ts.Width, ts.Height);
                align = StringAlignment.Center;
            }
            else
            {
                // Short radial leg, then a horizontal leg to the label (iocomp "Medium" / "Low").
                var sign = cos >= 0 ? 1 : -1;
                elbow = new PointF(dot.X + cos * size * 0.06f, dot.Y + sin * size * 0.06f);
                end = new PointF(elbow.X + sign * size * 0.06f, elbow.Y);
                text = sign > 0
                    ? new RectangleF(end.X + 2, end.Y - ts.Height / 2, ts.Width, ts.Height)
                    : new RectangleF(end.X - 2 - ts.Width, end.Y - ts.Height / 2, ts.Width, ts.Height);
                align = sign > 0 ? StringAlignment.Near : StringAlignment.Far;
            }

            labels[i] = new LabelLayout(dot, elbow, end, text, align);
            bounds = RectangleF.Union(bounds, text);
            bounds = RectangleF.Union(bounds, new RectangleF(dot.X - 3, dot.Y - 3, 6, 6));
        }
        return labels;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        var size = (float)Math.Min(Width, Height);
        if (size < 20) return;

        // Lay out once, shrink if the labels do not fit, then centre the whole drawing.
        LabelLayout[] labels;
        RectangleF bounds;
        Font normal, big;
        while (true)
        {
            normal = new Font(Font.FontFamily, Math.Max(7f, size * 0.1f), FontStyle.Regular, GraphicsUnit.Pixel);
            big = new Font(Font.FontFamily, Math.Max(8f, size * 0.15f), FontStyle.Regular, GraphicsUnit.Pixel);
            labels = LayoutLabels(g, size, normal, big, out bounds);
            var fit = Math.Min((Width - 6) / bounds.Width, (Height - 6) / bounds.Height);
            if (fit >= 1 || size < 30) break;
            normal.Dispose();
            big.Dispose();
            size *= Math.Max(0.5f, fit);
        }

        using (normal)
        using (big)
        {
            _pivot = new PointF(Width / 2f - (bounds.Left + bounds.Width / 2), Height / 2f - (bounds.Top + bounds.Height / 2));
            g.TranslateTransform(_pivot.X, _pivot.Y);

            using var line = new Pen(Color.FromArgb(40, 40, 40));
            using var dotBrush = new SolidBrush(Color.Cyan);
            using var textBrush = new SolidBrush(ForeColor);
            for (var i = 0; i < labels.Length; i++)
            {
                var l = labels[i];
                g.DrawLine(line, l.Dot, l.Elbow);
                g.DrawLine(line, l.Elbow, l.End);
                using var sf = new StringFormat { Alignment = l.Align, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(_items[i], i == _selectedIndex ? big : normal, textBrush, l.Text, sf);
            }

            // Pointer bar: tip at the selected position's dot, through the pivot, tail slightly longer (as iocomp).
            var sel = AngleOf(_selectedIndex) * Math.PI / 180;
            var dir = new PointF((float)Math.Cos(sel), (float)Math.Sin(sel));
            var r = labels[_selectedIndex].Dot;
            var len = (float)Math.Sqrt(r.X * r.X + r.Y * r.Y);
            var p1 = new PointF(dir.X * len, dir.Y * len);
            var p2 = new PointF(-p1.X * 1.15f, -p1.Y * 1.15f);
            var half = Math.Max(2f, size * 0.026f);
            var nx = -dir.Y * half;
            var ny = dir.X * half;
            PointF[] bar = [new(p1.X + nx, p1.Y + ny), new(p2.X + nx, p2.Y + ny), new(p2.X - nx, p2.Y - ny), new(p1.X - nx, p1.Y - ny)];
            using (var fill = new LinearGradientBrush(new PointF(nx, ny), new PointF(-nx, -ny), Color.White, Color.FromArgb(236, 236, 236)))
                g.FillPolygon(fill, bar);
            using (var outline = new Pen(Color.FromArgb(160, 160, 160)))
                g.DrawPolygon(outline, bar);

            foreach (var l in labels)
                g.FillEllipse(dotBrush, l.Dot.X - 2.5f, l.Dot.Y - 2.5f, 5, 5);

            g.ResetTransform();
        }

        if (Focused)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(1, 1, Width - 3, Height - 3));
    }
}
