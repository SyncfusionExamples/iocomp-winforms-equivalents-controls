using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace PocApp.Infrastructure;

/// <summary>
/// Visual regression check: compares freshly rendered page snapshots with committed baselines.
/// A pixel counts as changed when any channel differs by more than <see cref="ChannelTolerance"/>
/// (absorbs anti-aliasing noise); a page fails when the changed share exceeds <see cref="FailPercent"/>.
/// </summary>
public static class SnapshotComparer
{
    public const int ChannelTolerance = 24;
    public const double FailPercent = 0.5;

    public sealed record Result(string Page, string Status, double ChangedPercent, string? DiffImage);

    public static IReadOnlyList<Result> Compare(string baselineDir, string currentDir)
    {
        var results = new List<Result>();
        var names = Directory.GetFiles(baselineDir, "*.png").Select(Path.GetFileName)
            .Union(Directory.GetFiles(currentDir, "*.png").Select(Path.GetFileName))
            .Where(n => n is not null && !n.EndsWith(".diff.png", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            var basePath = Path.Combine(baselineDir, name!);
            var curPath = Path.Combine(currentDir, name!);
            if (!File.Exists(basePath)) { results.Add(new(name!, "NEW (no baseline)", 100, null)); continue; }
            if (!File.Exists(curPath)) { results.Add(new(name!, "MISSING", 100, null)); continue; }

            using var a = new Bitmap(basePath);
            using var b = new Bitmap(curPath);
            if (a.Size != b.Size)
            {
                results.Add(new(name!, $"FAIL (size {a.Width}x{a.Height} -> {b.Width}x{b.Height})", 100, null));
                continue;
            }

            var (changed, diff) = Diff(a, b);
            var pct = 100.0 * changed / (a.Width * (double)a.Height);
            string? diffPath = null;
            if (changed > 0)
            {
                diffPath = Path.Combine(currentDir, Path.GetFileNameWithoutExtension(name) + ".diff.png");
                diff.Save(diffPath, ImageFormat.Png);
            }
            diff.Dispose();
            results.Add(new(name!, pct > FailPercent ? "FAIL" : changed > 0 ? "PASS (minor)" : "PASS", pct, diffPath));
        }
        return results;
    }

    /// <summary>Changed-pixel count, plus an image: baseline faded to grey, changed pixels in magenta.</summary>
    private static (int Changed, Bitmap Diff) Diff(Bitmap a, Bitmap b)
    {
        var rect = new Rectangle(0, 0, a.Width, a.Height);
        var pa = Pixels(a, rect);
        var pb = Pixels(b, rect);
        var outPx = new int[pa.Length];
        var changed = 0;
        for (var i = 0; i < pa.Length; i++)
        {
            int x = pa[i], y = pb[i];
            var d = Math.Max(Math.Abs((x & 0xFF) - (y & 0xFF)),
                    Math.Max(Math.Abs(((x >> 8) & 0xFF) - ((y >> 8) & 0xFF)), Math.Abs(((x >> 16) & 0xFF) - ((y >> 16) & 0xFF))));
            if (d > ChannelTolerance)
            {
                changed++;
                outPx[i] = unchecked((int)0xFFFF00FF);
            }
            else
            {
                var grey = 200 + (((x & 0xFF) + ((x >> 8) & 0xFF) + ((x >> 16) & 0xFF)) / 3) * 55 / 255;
                outPx[i] = unchecked((int)0xFF000000) | (grey << 16) | (grey << 8) | grey;
            }
        }
        var diff = new Bitmap(a.Width, a.Height, PixelFormat.Format32bppArgb);
        var data = diff.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(outPx, 0, data.Scan0, outPx.Length);
        diff.UnlockBits(data);
        return (changed, diff);
    }

    private static int[] Pixels(Bitmap bmp, Rectangle rect)
    {
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var px = new int[rect.Width * rect.Height];
            for (var row = 0; row < rect.Height; row++)
                Marshal.Copy(data.Scan0 + row * data.Stride, px, row * rect.Width, rect.Width);
            return px;
        }
        finally { bmp.UnlockBits(data); }
    }

    public static string Report(IReadOnlyList<Result> results, string baselineDir, string currentDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Snapshot comparison");
        sb.AppendLine();
        sb.AppendLine($"Baseline: `{baselineDir}`  Current: `{currentDir}`");
        sb.AppendLine($"Rule: a pixel changes when a channel differs by more than {ChannelTolerance}; a page fails above {FailPercent}% changed pixels.");
        sb.AppendLine();
        sb.AppendLine("| Page | Status | Changed pixels | Diff image |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var r in results)
            sb.AppendLine($"| {r.Page} | {r.Status} | {r.ChangedPercent:0.###}% | {(r.DiffImage is null ? "-" : Path.GetFileName(r.DiffImage))} |");
        return sb.ToString();
    }
}
