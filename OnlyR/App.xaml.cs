using System;
using System.IO;
using System.Threading;
using System.Windows;
using AutoMapper;
using OnlyR.Model;
using OnlyR.Utils;
using Serilog;

namespace OnlyR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private Mutex _appMutex;
        private readonly string _appString = "OnlyRAudioRecording";

        protected override void OnStartup(StartupEventArgs e)
        {
            if (AnotherInstanceRunning())
            {
                Shutdown();
            }
            else
            {
                string logsDirectory = FileUtils.GetLogFolder();

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.RollingFile(Path.Combine(logsDirectory, "log-{Date}.txt"), retainedFileCountLimit: 28)
                    .CreateLogger();

                Log.Logger.Information("==== Launched ====");

                ConfigureAutoMapper();
            }
        }

        private static void ConfigureAutoMapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<ObjectMappingProfile>();
            });
        }

        private bool AnotherInstanceRunning()
        {
            _appMutex = new Mutex(true, _appString, out var newInstance);
            return !newInstance;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _appMutex?.Dispose();
            Log.Logger.Information("==== Exit ====");
        }

    }
}
