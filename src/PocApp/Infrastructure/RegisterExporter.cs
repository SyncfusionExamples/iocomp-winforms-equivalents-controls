using System.Text;

namespace PocApp.Infrastructure;

/// <summary>Writes the internal gap register and the customer-facing summary from the same catalog.</summary>
public static class RegisterExporter
{
    private const string LookNote =
        "Look = how close the best live view is to the iocomp image (close / acceptable / different / n/a for placeholders). " +
        "It is one reviewer's judgement from rendered snapshots at 100% scaling, not a measurement.";

    public static string InternalMarkdown(IReadOnlyList<ControlEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Gap Register (INTERNAL - Syncfusion only)");
        sb.AppendLine();
        sb.AppendLine("Generated from `ControlCatalog`. Priorities and FR flags are proposals for product owners to confirm.");
        sb.AppendLine("Effort: every figure is an estimate; person-days were not timed, including for the built prototypes.");
        sb.AppendLine(LookNote);
        sb.AppendLine();
        AppendCounts(sb, entries);

        sb.AppendLine("## Product FR candidates");
        sb.AppendLine();
        sb.AppendLine("| Control | FR title | Priority | Effort | Built | Approach | Look | Notes |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var e in entries.Where(x => x.FrCandidate).OrderBy(x => x.Priority).ThenBy(x => x.Name))
            sb.AppendLine($"| {e.Name} | {e.FrTitle} | {e.Priority} | {e.EffortText} | {(e.BuiltText is "" ? "no" : e.BuiltText)} | {Approach(e)} | {Look(e)} | {e.InternalNotes} |");

        sb.AppendLine();
        sb.AppendLine("## Extend (E) controls");
        sb.AppendLine();
        sb.AppendLine("| Control | Syncfusion control | Gap | Priority | Effort | Approach | Look | Notes |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var e in entries.Where(x => x.Rating == Rating.E))
            sb.AppendLine($"| {e.Name} | {e.SyncfusionControl} | {e.GapSummary} | {e.Priority} | {e.EffortText} | {Approach(e)} | {Look(e)} | {e.InternalNotes} |");

        sb.AppendLine();
        sb.AppendLine("## As-is (A) controls");
        sb.AppendLine();
        sb.AppendLine("| Control | Syncfusion control | Approach | Look | Notes |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var e in entries.Where(x => x.Rating == Rating.A))
            sb.AppendLine($"| {e.Name} | {e.SyncfusionControl} | {Approach(e)} | {Look(e)} | {e.InternalNotes} |");

        return sb.ToString();
    }

    public static string CustomerMarkdown(IReadOnlyList<ControlEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# iocomp to Syncfusion WinForms: Migration Summary");
        sb.AppendLine();
        sb.AppendLine("A = available as-is, E = available with customization, N = no direct equivalent today.");
        sb.AppendLine("For N controls, a Syncfusion stand-in is an existing Syncfusion control with custom drawing that keeps its state and events; a prototype is a new control written for this demo.");
        sb.AppendLine("Approach = how the demo achieves it. Effort ranges are estimates.");
        sb.AppendLine(LookNote);
        sb.AppendLine();
        AppendCounts(sb, entries);

        sb.AppendLine("| iocomp control | Rating | Syncfusion control | Approach | Look | Notes | Effort to close |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var e in entries)
            sb.AppendLine($"| {e.Name} | {e.Rating} | {e.SyncfusionControl} | {Approach(e)} | {Look(e)} | {e.GapSummary} | {e.EffortText} |");

        return sb.ToString();
    }

    public static string SummaryLine(IReadOnlyList<ControlEntry> entries) =>
        $"Total controls: {entries.Count} | As-is: {entries.Count(x => x.Rating == Rating.A)} | " +
        $"Extend: {entries.Count(x => x.Rating == Rating.E)} | No equivalent: {entries.Count(x => x.Rating == Rating.N)} " +
        $"({entries.Count(x => x.BuiltInPoc)} prototypes, {entries.Count(x => x.IsSyncfusionStandIn)} Syncfusion stand-ins) | Look: " +
        string.Join(", ", new[] { Fidelity.Close, Fidelity.Acceptable, Fidelity.Different, Fidelity.None }
            .Select(f => $"{ControlCard.FidelityText(f)} {entries.Count(x => x.Fidelity == f)}"));

    public static string ApproachLine(IReadOnlyList<ControlEntry> entries) =>
        "Approach: " + string.Join(", ", Enum.GetValues<Approach>()
            .Select(a => $"{ControlCard.ApproachText(a).ToLowerInvariant()} {entries.Count(x => x.Approach == a)}"));

    private static string Approach(ControlEntry e) => ControlCard.ApproachText(e.Approach);

    private static string Look(ControlEntry e) => ControlCard.FidelityText(e.Fidelity);

    private static void AppendCounts(StringBuilder sb, IReadOnlyList<ControlEntry> entries)
    {
        sb.AppendLine(SummaryLine(entries));
        sb.AppendLine();
        sb.AppendLine(ApproachLine(entries));
        sb.AppendLine();
    }
}
