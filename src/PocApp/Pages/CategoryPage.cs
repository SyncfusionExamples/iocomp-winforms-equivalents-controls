using PocApp.Infrastructure;

namespace PocApp.Pages;

/// <summary>Scrollable list of cards for one catalog category.</summary>
public sealed class CategoryPage : UserControl
{
    public CategoryPage(string category)
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8),
        };

        foreach (var entry in ControlCatalog.All.Where(e => e.Category == category))
        {
            flow.Controls.Add(new ControlCard(
                entry,
                CardFactories.DefaultFor(entry.Name),
                CardFactories.RestyledFor(entry.Name)));
        }

        Controls.Add(flow);
    }
}
