using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using OnlyR.ViewModel.Messages;
using OnlyR.Model;
using OnlyR.AutoUpdates;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.PurgeRecordings;
using OnlyR.Pages;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.Utils;

namespace OnlyR.ViewModel
{
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

        public FrameworkElement? CurrentPage
        {
            get => _currentPage;
            set
            {
                if (!ReferenceEquals(_currentPage, value))
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
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

        private void OnAlwaysOnTopChanged(object recipient, AlwaysOnTopChanged obj)
        {
            OnPropertyChanged(nameof(AlwaysOnTop));
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
                var latestVersion = VersionDetection.GetLatestReleaseVersion();
                if (latestVersion != null && latestVersion > VersionDetection.GetCurrentVersion())
                {
                    // there is a new version....
                    _snackbarService.Enqueue("Update available", Properties.Resources.VIEW, LaunchWebPage);
                }
            });
        }

        private void LaunchWebPage()
        {
            var psi = new ProcessStartInfo
            {
                FileName = VersionDetection.LatestReleaseUrl,
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
}