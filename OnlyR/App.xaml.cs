using Microsoft.Extensions.DependencyInjection;
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
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using OnlyR.Model;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;

namespace OnlyR
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public partial class App
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly string _appString = "OnlyRAudioRecording";
        private Mutex? _appMutex;

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
            ApplyStartupTheme();

            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
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
            SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
            _appMutex?.Dispose();
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
        
        internal static void ApplyTheme(AppTheme mode)
        {
            var isDark = mode switch
            {
                AppTheme.Dark => true,
                AppTheme.System => SystemThemeHelper.IsSystemDarkTheme(),
                _ => false,
            };

            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
            paletteHelper.SetTheme(theme);
        }

        private static void ApplyStartupTheme()
        {
            var optionsService = Ioc.Default.GetService<IOptionsService>();
            if (optionsService != null)
            {
                ApplyTheme(optionsService.Options.AppTheme ?? AppTheme.System);
            }
        }

        private static void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var optionsService = Ioc.Default.GetService<IOptionsService>();
                if (optionsService?.Options.AppTheme == AppTheme.System)
                {
                    ApplyTheme(AppTheme.System);
                }
            }
        }

        private bool AnotherInstanceRunning()
        {
            _appMutex = new Mutex(true, _appString, out var newInstance);
            return !newInstance;
        }
    }
}
