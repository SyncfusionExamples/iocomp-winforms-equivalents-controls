using System.ComponentModel;

namespace HmiControls;

/// <summary>
/// Grid of square LEDs painted by one control (prototype for the iocomp MatrixLeds).
/// One control for the whole grid, not one child control per cell, so large grids stay cheap.
/// </summary>
public class MatrixLeds : Control
{
    private int _rows = 5;
    private int _columns = 5;
    private bool[] _states = new bool[25];
    private Color _onColor = Color.Lime;
    private Color _offColor = Color.FromArgb(0, 127, 0);

    public MatrixLeds()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        TabStop = true;
        Size = new Size(128, 128);
    }

    /// <summary>Raised when a cell changes; the argument is the cell index (row * Columns + column).</summary>
    public event EventHandler<int>? CellChanged;

    /// <summary>When true, clicking a cell toggles it.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool Interactive { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Rows { get => _rows; set => ResizeGrid(Math.Clamp(value, 1, 64), _columns); }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Columns { get => _columns; set => ResizeGrid(_rows, Math.Clamp(value, 1, 64)); }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OnColor { get => _onColor; set { _onColor = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OffColor { get => _offColor; set { _offColor = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool this[int row, int column]
    {
        get => _states[row * _columns + column];
        set
        {
            var i = row * _columns + column;
            if (_states[i] == value) return;
            _states[i] = value;
            Invalidate(CellRect(row, column));
            CellChanged?.Invoke(this, i);
        }
    }

    private void ResizeGrid(int rows, int columns)
    {
        var old = _states;
        var next = new bool[rows * columns];
        for (var r = 0; r < Math.Min(rows, _rows); r++)
            for (var c = 0; c < Math.Min(columns, _columns); c++)
                next[r * columns + c] = old[r * _columns + c];
        (_rows, _columns, _states) = (rows, columns, next);
        Invalidate();
    }

    private Rectangle CellRect(int row, int column)
    {
        var w = Width / (float)_columns;
        var h = Height / (float)_rows;
        var gap = Math.Max(1, (int)(Math.Min(w, h) * 0.06f));
        return Rectangle.FromLTRB((int)(column * w) + gap, (int)(row * h) + gap, (int)((column + 1) * w) - gap, (int)((row + 1) * h) - gap);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var back = new SolidBrush(Color.FromArgb(200, 200, 200));
        e.Graphics.FillRectangle(back, ClientRectangle);
        using var on = new SolidBrush(_onColor);
        using var off = new SolidBrush(_offColor);
        for (var r = 0; r < _rows; r++)
            for (var c = 0; c < _columns; c++)
                e.Graphics.FillRectangle(_states[r * _columns + c] ? on : off, CellRect(r, c));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!Interactive || e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
        var c = Math.Clamp(e.X * _columns / Math.Max(1, Width), 0, _columns - 1);
        var r = Math.Clamp(e.Y * _rows / Math.Max(1, Height), 0, _rows - 1);
        this[r, c] = !this[r, c];
    }
}
