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

        var form = new MainForm();
        var snapshotArg = Array.IndexOf(args, "--snapshots");
        if (snapshotArg >= 0 && snapshotArg + 1 < args.Length)
            form.SaveSnapshots(args[snapshotArg + 1]);
        var benchArg = Array.IndexOf(args, "--benchmark");
        if (benchArg >= 0 && benchArg + 1 < args.Length)
            form.RunBenchmarkMatrix(args[benchArg + 1], 6, Array.IndexOf(args, "--improve") >= 0);
        Application.Run(form);
    }
}
