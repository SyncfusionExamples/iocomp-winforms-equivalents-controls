using Syncfusion.Licensing;

namespace PocApp;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var key = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(key);
        }

        ApplicationConfiguration.Initialize();
        Cards.Phase2Cards.Register();
        Cards.Phase3Cards.Register();
        Cards.Phase4Cards.Register();
        Cards.Phase5Cards.Register();
        var exportArg = Array.IndexOf(args, "--export");
        if (exportArg >= 0 && exportArg + 1 < args.Length)
        {
            var folder = args[exportArg + 1];
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "GAP_REGISTER_INTERNAL.md"), Infrastructure.RegisterExporter.InternalMarkdown(Infrastructure.ControlCatalog.All));
            File.WriteAllText(Path.Combine(folder, "MIGRATION_SUMMARY_CUSTOMER.md"), Infrastructure.RegisterExporter.CustomerMarkdown(Infrastructure.ControlCatalog.All));
            return;
        }

        var snapshotArg = Array.IndexOf(args, "--snapshots");
        var compareArg = Array.IndexOf(args, "--compare");
        var selfTestArg = Array.IndexOf(args, "--selftest");
        var renderOnly = (snapshotArg >= 0 && snapshotArg + 1 < args.Length) || (compareArg >= 0 && compareArg + 2 < args.Length);
        if (renderOnly || selfTestArg >= 0)
        {
            // Repeatable output: fixed signal values, fixed clock time, no free-running animations.
            Infrastructure.SignalSimulator.Shared.Freeze(now: new DateTime(2026, 1, 1, 10, 9, 30));
            HmiControls.Animation.Enabled = false;
        }

        if (selfTestArg >= 0 && selfTestArg + 1 < args.Length)
        {
            Environment.ExitCode = Infrastructure.InteractionSelfTest.Run(args[selfTestArg + 1]);
            return;
        }

        var form = new MainForm();
        if (snapshotArg >= 0 && snapshotArg + 1 < args.Length)
            form.SaveSnapshots(args[snapshotArg + 1]);
        if (compareArg >= 0 && compareArg + 2 < args.Length)
        {
            var baseline = args[compareArg + 1];
            var current = args[compareArg + 2];
            form.SaveSnapshots(current, () =>
            {
                var results = Infrastructure.SnapshotComparer.Compare(baseline, current);
                File.WriteAllText(Path.Combine(current, "COMPARE.md"), Infrastructure.SnapshotComparer.Report(results, baseline, current));
                Environment.ExitCode = results.Any(r => !r.Status.StartsWith("PASS")) ? 1 : 0;
            });
        }
        Application.Run(form);
    }
}
