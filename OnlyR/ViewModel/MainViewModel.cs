using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using OnlyR.AutoUpdates;
using OnlyR.Model;
using OnlyR.Pages;
using OnlyR.Services.Audio;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.PurgeRecordings;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OnlyR.ViewModel;

/// <summary>
/// Main View model. The main view is just a container for pages
/// </summary>
public class MainViewModel : ObservableObject
{
    private readonly Dictionary<string, FrameworkElement> _pages;
    private readonly IOptionsService _optionsService;
    private readonly IAudioService _audioService;
    private readonly ISnackbarService _snackbarService;
    private readonly IPurgeRecordingsService _purgeRecordingsService;
    private readonly (string TempPath, string FinalPath)? _unfinishedRecordingFileFoundOnStartup;
    private FrameworkElement? _currentPage;
    private bool _exitPending;
    private int _exitFinalised;

    private static string LatestReleaseUrl => "https://github.com/AntonyCorbett/OnlyR/releases/latest";

    public MainViewModel(
        IAudioService audioService,
        IOptionsService optionsService,
        ICommandLineService commandLineService,
        IRecordingDestinationService destService,
        ICopyRecordingsService copyRecordingsService,
        ISnackbarService snackbarService,
        IPurgeRecordingsService purgeRecordingsService,
        ISilenceService silenceService)
    {
        if (commandLineService.NoGpu)
        {
            // disable hardware (GPU) rendering so that it's all done by the CPU...
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }

        // subscriptions...
        WeakReferenceMessenger.Default.Register<NavigateMessage>(this, OnNavigate);
        WeakReferenceMessenger.Default.Register<AlwaysOnTopChanged>(this, OnAlwaysOnTopChanged);
        WeakReferenceMessenger.Default.Register<ThemeModeChanged>(this, OnThemeModeChanged);

        _pages = new Dictionary<string, FrameworkElement>();

        _optionsService = optionsService;
        _audioService = audioService;
        _snackbarService = snackbarService;
        _purgeRecordingsService = purgeRecordingsService;

        // set up pages...
        SetupPage(
            RecordingPageViewModel.PageName,
            new RecordingPage(),
            new RecordingPageViewModel(
                audioService,
                optionsService,
                commandLineService,
                destService,
                copyRecordingsService,
                snackbarService,
                silenceService));

        SetupPage(
            SettingsPageViewModel.PageName,
            new SettingsPage(),
            new SettingsPageViewModel(audioService, optionsService, commandLineService));

        RecordingPageModel.PropertyChanged += OnRecordingPagePropertyChanged;

        _unfinishedRecordingFileFoundOnStartup = GetUnfinishedRecordingPaths();

        var state = new RecordingPageNavigationState
        {
            ShowSplash = !optionsService.Options.StartMinimized,
            StartRecording = optionsService.Options.StartRecordingOnLaunch &&
                             _unfinishedRecordingFileFoundOnStartup == null
        };

        GetVersionData();

        WeakReferenceMessenger.Default.Send(new NavigateMessage(
            null, RecordingPageViewModel.PageName, state));

        FixAnyUnfinishedRecording();
    }

    public string? CurrentPageName { get; private set; }

    public ISnackbarMessageQueue TheSnackbarMessageQueue => _snackbarService.TheSnackbarMessageQueue;

    public bool AlwaysOnTop => _optionsService.Options.AlwaysOnTop;

    public bool IsRecording => RecordingPageModel.IsRecordingOrStopping;

    public bool CanRecord => RecordingPageModel.CanRecord;

    private RecordingPageViewModel RecordingPageModel =>
        (RecordingPageViewModel)_pages[RecordingPageViewModel.PageName].DataContext;

    public FrameworkElement? CurrentPage
    {
        get => _currentPage;
        set
        {
            if (ReferenceEquals(_currentPage, value))
                return;

            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public void Closing(object sender, CancelEventArgs e)
    {
        var recordingPageModel = (RecordingPageViewModel)_pages[RecordingPageViewModel.PageName].DataContext;

        if (_optionsService.Options.AllowCloseWhenRecording)
        {
            if (recordingPageModel.IsRecordingOrStopping)
            {
                e.Cancel = true;
                _audioService.StoppedEvent += RecordingStoppedDuringAppClose;
                _audioService.StopRecording(_optionsService.Options.FadeOut);
            }
        }
        else
        {
            // prevent window closing when recording...
            recordingPageModel.Closing(sender, e);

            if (!e.Cancel)
            {
                WeakReferenceMessenger.Default.Send(new BeforeShutDownMessage(CurrentPageName));
                (_audioService as IDisposable)?.Dispose();
            }
        }

        if (!e.Cancel)
        {
            _purgeRecordingsService.Close();
        }
    }

    public void StartRecording() => RecordingPageModel.StartRecordingCommand.Execute(null);

    public void StopRecording() => RecordingPageModel.StopRecordingCommand.Execute(null);

    public void OpenRecordingsFolder() => RecordingPageModel.ShowRecordingsCommand.Execute(null);

    /// <summary>
    /// Stops any recording in progress (so that the recording file is finalised)
    /// and then shuts the application down.
    /// </summary>
    /// <returns>
    /// true if the shutdown has been started or is pending; false if the exit is not
    /// allowed because a recording is in progress and closing while recording is disabled.
    /// </returns>
    public bool RequestExit()
    {
        if (App.IsShuttingDown)
        {
            return true;
        }

        if (!_optionsService.Options.AllowCloseWhenRecording &&
            (IsRecording || _audioService.HasPendingRecording))
        {
            return false;
        }

        if (!_audioService.HasPendingRecording)
        {
            ShutDown();
            return true;
        }

        if (_exitPending)
        {
            return true;
        }

        _exitPending = true;
        _ = Interlocked.Exchange(ref _exitFinalised, 0);
        _audioService.RecordingFinalisedEvent += OnRecordingFinalisedBeforeExit;

        if (!RecordingPageModel.TryStopRecording())
        {
            _audioService.RecordingFinalisedEvent -= OnRecordingFinalisedBeforeExit;
            _exitPending = false;
            return false;
        }

        if (!_audioService.HasPendingRecording &&
            Volatile.Read(ref _exitFinalised) == 0)
        {
            // the recording was finalised before the event subscription (or synchronously)...
            OnRecordingFinalisedBeforeExit(this, EventArgs.Empty);
        }

        return true;
    }

    private void OnAlwaysOnTopChanged(object recipient, AlwaysOnTopChanged obj)
    {
        OnPropertyChanged(nameof(AlwaysOnTop));
    }

    private void OnThemeModeChanged(object recipient, ThemeModeChanged obj)
    {
        App.ApplyTheme(_optionsService.Options.AppTheme ?? AppTheme.System);
    }

    private void OnRecordingPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecordingPageViewModel.IsRecordingOrStopping))
        {
            OnPropertyChanged(nameof(IsRecording));
        }
        else if (e.PropertyName == nameof(RecordingPageViewModel.CanRecord))
        {
            OnPropertyChanged(nameof(CanRecord));
        }
    }

    private void OnRecordingFinalisedBeforeExit(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _exitFinalised, 1) != 0)
        {
            return;
        }

        _audioService.RecordingFinalisedEvent -= OnRecordingFinalisedBeforeExit;

        // the event may be raised on the audio capture thread, so hand the shutdown
        // to the UI thread without blocking the thread that finalised the recording
        _ = Application.Current.Dispatcher.BeginInvoke(new Action(ShutDown));
    }

    private static void ShutDown()
    {
        if (App.IsShuttingDown)
        {
            return;
        }

        App.BeginShutdown();
        Application.Current.Shutdown();
    }

    private void SetupPage(string pageName, FrameworkElement page, ObservableObject pageModel)
    {
        page.DataContext = pageModel;
        _pages.Add(pageName, page);
    }

    private void OnNavigate(object recipient, NavigateMessage message)
    {
        CurrentPage = _pages[message.TargetPageName];
        CurrentPageName = message.TargetPageName;
        ((IPage)CurrentPage.DataContext).Activated(message.State);
    }

    private void RecordingStoppedDuringAppClose(object? sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShutDownApplicationMessage());
    }

    private void GetVersionData()
    {
        Task.Delay(2000).ContinueWith(_ =>
        {
            if (ShouldShowUpdateNotification(
                    VersionDetection.GetCurrentVersion(),
                    VersionDetection.GetLatestReleaseVersion()))
            {
                _snackbarService.Enqueue("Update available", Properties.Resources.VIEW, LaunchWebPage);
            }
        });
    }

    internal static bool ShouldShowUpdateNotification(Version? current, Version? latest)
    {
        return latest != null && current != null && latest > current;
    }

    private void LaunchWebPage()
    {
        var psi = new ProcessStartInfo
        {
            FileName = LatestReleaseUrl,
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    private void FixAnyUnfinishedRecording()
    {
        if (_unfinishedRecordingFileFoundOnStartup == null)
        {
            return;
        }

        if (File.Exists(_unfinishedRecordingFileFoundOnStartup.Value.FinalPath))
        {
            return;
        }

        FileUtils.MoveFile(
            _unfinishedRecordingFileFoundOnStartup.Value.TempPath,
            _unfinishedRecordingFileFoundOnStartup.Value.FinalPath);

        _snackbarService.Enqueue(Properties.Resources.RECORDING_SALVAGED);
    }

    private (string tempPath, string FinalPath)? GetUnfinishedRecordingPaths()
    {
        var tempPath = _optionsService.Options.UnfinishedRecordingTempPath;
        var finalPath = _optionsService.Options.UnfinishedRecordingFinalPath;

        if (!string.IsNullOrEmpty(tempPath) &&
            !string.IsNullOrEmpty(finalPath) &&
            File.Exists(tempPath))
        {
            return (tempPath, finalPath);
        }

        return null;
    }
}