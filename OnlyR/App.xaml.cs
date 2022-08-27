using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using OnlyR.Services.Audio;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.PurgeRecordings;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.ViewModel;
using Serilog;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;

namespace OnlyR
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable.  Disposed on exit.
    public partial class App
#pragma warning restore CA1001 // Types that own disposable fields should be disposable.  Disposed on exit.
    {
        private readonly string _appString = "OnlyRAudioRecording";
        private Mutex _appMutex = new ();

        protected override void OnStartup(StartupEventArgs e)
        {
            if (AnotherInstanceRunning())
            {
                Shutdown();
            }
            else
            {
                ConfigureLogger();
            }

            ConfigureServices();

            Current.DispatcherUnhandledException += CurrentDispatcherUnhandledException;
        }

        private void CurrentDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // unhandled exceptions thrown from UI thread
            e.Handled = true;
            Log.Logger.Fatal(e.Exception, "Unhandled exception");
            Current.Shutdown();
        }

        private static void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddSingleton<IOptionsService, OptionsService>();
            serviceCollection.AddSingleton<ICommandLineService, CommandLineService>();
            serviceCollection.AddSingleton<IRecordingDestinationService, RecordingDestinationService>();
            serviceCollection.AddSingleton<IAudioService, AudioService>();
            serviceCollection.AddSingleton<ICopyRecordingsService, CopyRecordingsService>();
            serviceCollection.AddSingleton<IDriveEjectionService, DriveEjectionService>();
            serviceCollection.AddSingleton<ISnackbarService, SnackbarService>();
            serviceCollection.AddSingleton<IPurgeRecordingsService, PurgeRecordingsService>();
            serviceCollection.AddSingleton<ISilenceService, SilenceService>();
            serviceCollection.AddSingleton<MainViewModel>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _appMutex.Dispose();
            Log.Logger.Information("==== Exit ====");
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SessionEndingMessage(e));
            base.OnSessionEnding(e);
        }

        private static void ConfigureLogger()
        {
            var logsDirectory = FileUtils.GetLogFolder();

#if DEBUG
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsDirectory, "log-.txt"), retainedFileCountLimit: 28, rollingInterval: RollingInterval.Day)
                .CreateLogger();
#else
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(Path.Combine(logsDirectory, "log-.txt"), retainedFileCountLimit: 28, rollingInterval: RollingInterval.Day)
                .CreateLogger();
#endif

            Log.Logger.Information("==== Launched ====");
        }
        
        private bool AnotherInstanceRunning()
        {
            _appMutex = new Mutex(true, _appString, out var newInstance);
            return !newInstance;
        }
    }
}
