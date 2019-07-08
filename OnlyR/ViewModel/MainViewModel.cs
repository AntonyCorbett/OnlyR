namespace OnlyR.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Messaging;
    using MaterialDesignThemes.Wpf;
    using Messages;
    using Model;
    using OnlyR.AutoUpdates;
    using OnlyR.Services.PurgeRecordings;
    using Pages;
    using Services.Audio;
    using Services.Options;
    using Services.RecordingCopies;
    using Services.RecordingDestination;
    using Services.Snackbar;

    /// <summary>
    /// Main View model. The main view is just a container for pages
    /// <para>
    /// See http://www.galasoft.ch/mvvm for details of the mvvm light framework
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly Dictionary<string, FrameworkElement> _pages;
        private readonly IOptionsService _optionsService;
        private readonly IAudioService _audioService;
        private readonly ISnackbarService _snackbarService;
        private readonly IPurgeRecordingsService _purgeRecordingsService;
        private string _currentPageName;
        private FrameworkElement _currentPage;

        public MainViewModel(
           IAudioService audioService,
           IOptionsService optionsService,
           ICommandLineService commandLineService,
           IRecordingDestinationService destService,
           ICopyRecordingsService copyRecordingsService,
           ISnackbarService snackbarService,
           IPurgeRecordingsService purgeRecordingsService)
        {
            if (commandLineService.NoGpu)
            {
                // disable hardware (GPU) rendering so that it's all done by the CPU...
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }

            // subscriptions...
            Messenger.Default.Register<NavigateMessage>(this, OnNavigate);
            Messenger.Default.Register<AlwaysOnTopChanged>(this, OnAlwaysOnTopChanged);

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
                    snackbarService));

            SetupPage(
                SettingsPageViewModel.PageName, 
                new SettingsPage(),
                new SettingsPageViewModel(audioService, optionsService, commandLineService));

            var state = new RecordingPageNavigationState
            {
                ShowSplash = true,
                StartRecording = optionsService.Options.StartRecordingOnLaunch,
            };

            GetVersionData();

            Messenger.Default.Send(new NavigateMessage(RecordingPageViewModel.PageName, state));
        }

        public ISnackbarMessageQueue TheSnackbarMessageQueue => _snackbarService.TheSnackbarMessageQueue;

        public bool AlwaysOnTop => _optionsService.Options.AlwaysOnTop;

        public FrameworkElement CurrentPage
        {
            get => _currentPage;
            set
            {
                if (!ReferenceEquals(_currentPage, value))
                {
                    _currentPage = value;
                    RaisePropertyChanged(nameof(CurrentPage));
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
                    Messenger.Default.Send(new BeforeShutDownMessage(_currentPageName));
                    (_audioService as IDisposable)?.Dispose();
                }
            }

            if (!e.Cancel)
            {
                _purgeRecordingsService.Close();
            }
        }

        private void OnAlwaysOnTopChanged(AlwaysOnTopChanged obj)
        {
            RaisePropertyChanged(nameof(AlwaysOnTop));
        }
        
        private void SetupPage(string pageName, FrameworkElement page, ViewModelBase pageModel)
        {
            page.DataContext = pageModel;
            _pages.Add(pageName, page);
        }
        
        private void OnNavigate(NavigateMessage message)
        {
            CurrentPage = _pages[message.TargetPage];
            _currentPageName = message.TargetPage;
            ((IPage)CurrentPage.DataContext).Activated(message.State);
        }

        private void RecordingStoppedDuringAppClose(object sender, EventArgs e)
        {
            Messenger.Default.Send(new ShutDownApplicationMessage());
        }

        private void GetVersionData()
        {
            Task.Delay(2000).ContinueWith(_ =>
            {
                var latestVersion = VersionDetection.GetLatestReleaseVersion();
                if (latestVersion != null)
                {
                    if (latestVersion > VersionDetection.GetCurrentVersion())
                    {
                        // there is a new version....
                        _snackbarService.Enqueue("Update available", Properties.Resources.VIEW, LaunchWebPage);
                    }
                }
            });
        }

        private void LaunchWebPage()
        {
            Process.Start(VersionDetection.LatestReleaseUrl);
        }
    }
}