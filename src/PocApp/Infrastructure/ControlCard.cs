namespace PocApp.Infrastructure;

/// <summary>
/// One card per iocomp control. Left: iocomp reference image. Right: live control slots.
/// A: one slot (default). E: two slots (default | restyled). N: badge plus stand-in or placeholder.
/// Phases 2-4 fill the slots through <see cref="CardFactories"/>.
/// </summary>
public sealed class ControlCard : Panel
{
    private static readonly Color BadgeA = Color.FromArgb(46, 125, 50);
    private static readonly Color BadgeE = Color.FromArgb(245, 124, 0);
    private static readonly Color BadgeN = Color.FromArgb(198, 40, 40);

    public ControlEntry Entry { get; }

    public ControlCard(ControlEntry entry, Func<Control>? defaultView, Func<Control>? restyledView)
    {
        Entry = entry;
        Size = new Size(760, 240);
        Margin = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.White;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(6),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(Header(), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 3);

        layout.Controls.Add(ReferenceImage(), 0, 1);

        var live1 = Slot(entry.Rating == Rating.E ? "Default" : "Syncfusion", defaultView, entry);
        layout.Controls.Add(live1, 1, 1);

        if (entry.Rating == Rating.E)
            layout.Controls.Add(Slot("Restyled", restyledView, entry), 2, 1);
        else
            layout.Controls.Add(NoteBox(), 2, 1);

        Controls.Add(layout);
    }

    private Control Header()
    {
        var badgeColor = Entry.Rating switch { Rating.A => BadgeA, Rating.E => BadgeE, _ => BadgeN };
        var badgeText = Entry.Rating switch
        {
            Rating.A => "A - as-is",
            Rating.E => "E - extend",
            _ when Entry.BuiltInPoc => "N - no equivalent (prototype built)",
            _ when Entry.IsSyncfusionStandIn => "N - no equivalent (stand-in)",
            _ => "N - no equivalent",
        };

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.Add(new Label
        {
            Text = Entry.Name,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 2, 12, 0),
        });
        panel.Controls.Add(new Label
        {
            Text = badgeText,
            ForeColor = Color.White,
            BackColor = badgeColor,
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 2, 12, 0),
        });
        panel.Controls.Add(new Label
        {
            // The badge already says "stand-in"; repeating it here pushed the Look chip off the card.
            Text = Entry.IsSyncfusionStandIn ? Entry.SyncfusionControl.Replace(" (stand-in)", "") : Entry.SyncfusionControl,
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 5, 12, 0),
        });
        panel.Controls.Add(Chip(ApproachText(Entry.Approach), Color.FromArgb(232, 236, 242), Color.FromArgb(50, 60, 80)));
        var (fidelityBack, fidelityFore) = FidelityColors(Entry.Fidelity);
        panel.Controls.Add(Chip("Look: " + FidelityText(Entry.Fidelity), fidelityBack, fidelityFore));
        return panel;
    }

    private static Label Chip(string text, Color back, Color fore) => new()
    {
        Text = text,
        AutoSize = true,
        BackColor = back,
        ForeColor = fore,
        Padding = new Padding(5, 2, 5, 2),
        Margin = new Padding(0, 3, 6, 0),
        Font = new Font("Segoe UI", 8f),
    };

    public static string ApproachText(Approach a) => a switch
    {
        Approach.Properties => "Properties only",
        Approach.Composition => "Composed",
        Approach.CustomRenderer => "Custom renderer",
        Approach.CustomPaint => "Custom paint",
        Approach.Prototype => "New control",
        _ => "Placeholder",
    };

    public static string FidelityText(Fidelity f) => f == Fidelity.None ? "n/a" : f.ToString().ToLowerInvariant();

    private static (Color Back, Color Fore) FidelityColors(Fidelity f) => f switch
    {
        Fidelity.Close => (Color.FromArgb(220, 240, 222), Color.FromArgb(27, 94, 32)),
        Fidelity.Acceptable => (Color.FromArgb(255, 243, 205), Color.FromArgb(120, 80, 0)),
        Fidelity.Different => (Color.FromArgb(253, 226, 226), Color.FromArgb(150, 30, 30)),
        _ => (Color.FromArgb(238, 238, 238), Color.DimGray),
    };

    private Control ReferenceImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "iocomp-dotnet-images", Entry.Name + ".png");
        var box = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = $"iocomp {Entry.Name} reference image",
        };
        if (File.Exists(path))
            box.Image = Image.FromFile(path);
        return box;
    }

    private static Control Slot(string caption, Func<Control>? factory, ControlEntry entry)
    {
        var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4) };
        var title = new Label { Text = caption, Dock = DockStyle.Top, Height = 18, ForeColor = Color.DimGray };
        Control content;
        if (factory is not null)
        {
            content = factory();
            content.Dock = DockStyle.Fill;
        }
        else
        {
            content = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(245, 245, 245),
                Text = entry.Rating == Rating.N
                    ? "Placeholder: no Syncfusion equivalent"
                    : "Not built yet",
            };
        }
        host.Controls.Add(content);
        host.Controls.Add(title);
        return host;
    }

    private Control NoteBox() => new Label
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(8),
        Text = Entry.GapSummary + (Entry.EffortSource == EffortSource.None ? "" : $"\r\n\r\nEffort to close: {Entry.EffortText}"),
        ForeColor = Color.FromArgb(60, 60, 60),
    };
}
