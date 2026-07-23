namespace RamosPartGenerator.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, args) => AppLog.Error("App.ThreadException", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception
                ?? new InvalidOperationException(args.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
            AppLog.Error("App.UnhandledException", exception, ("isTerminating", args.IsTerminating.ToString()));
        };

        var appVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        AppLog.Info("App.Start", ("version", appVersion), ("logPath", AppLog.CurrentLogPath));
        try
        {
            Application.Run(new MainForm());
        }
        finally
        {
            AppLog.Info("App.Exit");
        }
    }
}
