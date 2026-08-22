namespace ChurchProjector;

static class Program
{
    private static readonly string CrashLog = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MPH Songs", "crash.log");

    [STAThread]
    static void Main()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CrashLog)!);

        Application.ThreadException += (_, e) =>
        {
            File.AppendAllText(CrashLog,
                $"[{DateTime.Now}] ThreadException: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                File.AppendAllText(CrashLog,
                    $"[{DateTime.Now}] UnhandledException: {ex.Message}\n{ex.StackTrace}\n\n");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            File.AppendAllText(CrashLog,
                $"[{DateTime.Now}] TaskException: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
        };

        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            File.AppendAllText(CrashLog,
                $"[{DateTime.Now}] MainException: {ex.Message}\n{ex.StackTrace}\n\n");
            MessageBox.Show($"Startup error: {ex.Message}", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
