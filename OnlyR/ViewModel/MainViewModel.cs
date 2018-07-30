using OnlyR.Services.RecordingCopies;

namespace OnlyR.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Messaging;
    using Messages;
    using Model;
    using Pages;
    using Services.Audio;
    using Services.Options;
    using Services.RecordingDestination;

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
        private string _currentPageName;

        public MainViewModel(
           IAudioService audioService,
           IOptionsService optionsService,
           ICommandLineService commandLineService,
           IRecordingDestinationService destService,
           ICopyRecordingsService copyRecordingsService)
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

            // set up pages...
            SetupPage(
                RecordingPageViewModel.PageName, 
                new RecordingPage(),
                new RecordingPageViewModel(audioService, optionsService, commandLineService, destService, copyRecordingsService));

            SetupPage(
                SettingsPageViewModel.PageName, 
                new SettingsPage(),
                new SettingsPageViewModel(audioService, optionsService, commandLineService));

            var state = new RecordingPageNavigationState
            {
                ShowSplash = true,
                StartRecording = optionsService.Options.StartRecordingOnLaunch
            };

            Messenger.Default.Send(new NavigateMessage(RecordingPageViewModel.PageName, state));
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
        }

        private void OnAlwaysOnTopChanged(AlwaysOnTopChanged obj)
        {
            RaisePropertyChanged(nameof(AlwaysOnTop));
        }

        public bool AlwaysOnTop => _optionsService.Options.AlwaysOnTop;

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

        private FrameworkElement _currentPage;

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

        private void RecordingStoppedDuringAppClose(object sender, EventArgs e)
        {
            Messenger.Default.Send(new ShutDownApplicationMessage());
        }
    }
}