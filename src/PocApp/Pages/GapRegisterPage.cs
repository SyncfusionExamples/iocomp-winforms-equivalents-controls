using PocApp.Infrastructure;

namespace PocApp.Pages;

/// <summary>Filterable table of the catalog with export buttons.</summary>
public sealed class GapRegisterPage : UserControl
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    };

    private readonly ComboBox _filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly ComboBox _approach = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly ComboBox _fidelity = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly Label _summary = new() { AutoSize = true, Margin = new Padding(12, 6, 0, 0) };

    private readonly Label _totals = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        Padding = new Padding(10, 4, 10, 0),
        ForeColor = Color.FromArgb(50, 50, 50),
        Text = RegisterExporter.SummaryLine(ControlCatalog.All) + Environment.NewLine + RegisterExporter.ApproachLine(ControlCatalog.All),
    };

    public GapRegisterPage()
    {
        _filter.Items.AddRange(["All", "A", "E", "N"]);
        _filter.SelectedIndex = 0;
        _filter.AccessibleName = "Rating filter";
        _filter.SelectedIndexChanged += (_, _) => Reload();

        _approach.Items.Add("All");
        foreach (var a in Enum.GetValues<Approach>()) _approach.Items.Add(ControlCard.ApproachText(a));
        _approach.SelectedIndex = 0;
        _approach.AccessibleName = "Approach filter";
        _approach.SelectedIndexChanged += (_, _) => Reload();

        _fidelity.Items.Add("All");
        foreach (var f in Enum.GetValues<Fidelity>()) _fidelity.Items.Add(ControlCard.FidelityText(f));
        _fidelity.SelectedIndex = 0;
        _fidelity.AccessibleName = "Look filter";
        _fidelity.SelectedIndexChanged += (_, _) => Reload();

        var internalButton = new Button { Text = "Export internal register...", AutoSize = true };
        internalButton.Click += (_, _) => Export("GAP_REGISTER_INTERNAL.md", RegisterExporter.InternalMarkdown);

        var customerButton = new Button { Text = "Export customer summary...", AutoSize = true };
        customerButton.Click += (_, _) => Export("MIGRATION_SUMMARY_CUSTOMER.md", RegisterExporter.CustomerMarkdown);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 6, 8, 0) };
        bar.Controls.AddRange([new Label { Text = "Rating:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) }, _filter,
            new Label { Text = "Approach:", AutoSize = true, Margin = new Padding(8, 6, 4, 0) }, _approach,
            new Label { Text = "Look:", AutoSize = true, Margin = new Padding(8, 6, 4, 0) }, _fidelity,
            internalButton, customerButton, _summary]);

        Controls.Add(_grid);
        Controls.Add(_totals);
        Controls.Add(bar);
        Reload();
    }

    private static bool Matches(ComboBox box, string value) => box.SelectedItem?.ToString() is null or "All" || box.SelectedItem.ToString() == value;

    private void Reload()
    {
        var rows = ControlCatalog.All
            .Where(e => Matches(_filter, e.Rating.ToString())
                        && Matches(_approach, ControlCard.ApproachText(e.Approach))
                        && Matches(_fidelity, ControlCard.FidelityText(e.Fidelity)))
            .Select(e => new
            {
                e.Name,
                e.Category,
                Rating = e.Rating.ToString(),
                Syncfusion = e.SyncfusionControl,
                Approach = ControlCard.ApproachText(e.Approach),
                Look = ControlCard.FidelityText(e.Fidelity),
                Gap = e.GapSummary,
                Effort = e.EffortText,
                e.Priority,
                FR = e.FrCandidate ? e.FrTitle : "",
                Built = e.BuiltInPoc ? "yes" : "",
            })
            .ToList();

        _grid.DataSource = rows;
        _summary.Text = $"{rows.Count} of {ControlCatalog.All.Count} controls";
    }

    private static void Export(string defaultName, Func<IReadOnlyList<ControlEntry>, string> render)
    {
        using var dialog = new SaveFileDialog { FileName = defaultName, Filter = "Markdown (*.md)|*.md" };
        if (dialog.ShowDialog() == DialogResult.OK)
            File.WriteAllText(dialog.FileName, render(ControlCatalog.All));
    }
}
