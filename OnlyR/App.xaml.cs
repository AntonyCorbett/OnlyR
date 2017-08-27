using System.IO;
using System.Windows;
using OnlyR.Utils;
using Serilog;

namespace OnlyR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string logsDirectory = FileUtils.GetLogFolder();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.RollingFile(Path.Combine(logsDirectory, "log-{Date}.txt"), retainedFileCountLimit: 28)
                .CreateLogger();

            Log.Logger.Information("==== Launched ====");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Logger.Information("==== Exit ====");
        }
    }
}
