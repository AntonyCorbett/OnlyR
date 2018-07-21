namespace OnlyR.ViewModel
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Threading;
    using Core.Enums;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Messaging;
    using Messages;
    using Model;
    using Serilog;
    using Services.Audio;
    using Services.Options;
    using Services.RecordingDestination;
    using Utils;

    /// <summary>
    /// View model for Recording page. Contains properties that the Recording page 
    /// can data bind to, i.e. it has everything that is needed by the user during 
    /// interaction with the Recording page
    /// </summary>
    public class RecordingPageViewModel : ViewModelBase, IPage
    {
        private readonly IAudioService _audioService;
        private readonly IRecordingDestinationService _destinationService;
        private readonly IOptionsService _optionsService;
        private readonly string _commandLineIdentifier;
        private readonly ulong _safeMinBytesFree = 0x20000000;  // 0.5GB
        private readonly Stopwatch _stopwatch;
        private DispatcherTimer _splashTimer;

        private RecordingStatus _recordingStatus;
        private string _statusStr;
        private string _errorMsg;

        public RecordingPageViewModel(
            IAudioService audioService,
            IOptionsService optionsService,
            ICommandLineService commandLineService,
            IRecordingDestinationService destinationService)
        {
            Messenger.Default.Register<BeforeShutDownMessage>(this, OnShutDown);

            _commandLineIdentifier = commandLineService.OptionsIdentifier;
            _stopwatch = new Stopwatch();

            _audioService = audioService;
            _audioService.StartedEvent += AudioStartedHandler;
            _audioService.StoppedEvent += AudioStoppedHandler;
            _audioService.StopRequested += AudioStopRequestedHandler;
            _audioService.RecordingProgressEvent += AudioProgressHandler;

            _optionsService = optionsService;
            _destinationService = destinationService;
            _recordingStatus = RecordingStatus.NotRecording;

            _statusStr = Properties.Resources.NOT_RECORDING;

            // bind commands...
            StartRecordingCommand = new RelayCommand(StartRecording, CanExecuteStart);
            StopRecordingCommand = new RelayCommand(StopRecording, CanExecuteStop);
            NavigateSettingsCommand = new RelayCommand(NavigateSettings, CanExecuteNavigateSettings);
        }

        /// <summary>
        /// Responds to activation
        /// </summary>
        /// <param name="state">RecordingPageNavigationState object (or null)</param>
        public void Activated(object state)
        {
            // on display of page...
            var stateObj = (RecordingPageNavigationState)state;
            if (stateObj != null)
            {
                if (stateObj.StartRecording)
                {
                    StartRecording();
                }
                else if (stateObj.ShowSplash)
                {
                    DoSplash();
                }
            }
        }

        public void Closing(object sender, CancelEventArgs e)
        {
            // prevent window closing when recording...
            e.Cancel = RecordingStatus != RecordingStatus.NotRecording;
        }

        private static void NavigateSettings()
        {
            Messenger.Default.Send(new NavigateMessage(SettingsPageViewModel.PageName, null));
        }

        private void OnShutDown(BeforeShutDownMessage message)
        {
            // nothing to do
        }

        public static string PageName => "RecordingPage";

        private bool CanExecuteNavigateSettings()
        {
            return RecordingStatus == RecordingStatus.NotRecording;
        }

        private bool CanExecuteStop()
        {
            return RecordingStatus == RecordingStatus.Recording;
        }

        private bool CanExecuteStart()
        {
            return RecordingStatus == RecordingStatus.NotRecording;
        }

        private void AudioProgressHandler(object sender, Core.EventArgs.RecordingProgressEventArgs e)
        {
            VolumeLevelAsPercentage = e.VolumeLevelAsPercentage;
            RaisePropertyChanged(nameof(ElapsedTimeStr));

            if (_optionsService.Options.MaxRecordingTimeMins > 0 &&
                ElapsedTime.TotalSeconds > _optionsService.Options.MaxRecordingTimeMins * 60)
            {
                AutoStopRecording();
            }
        }

        private void AutoStopRecording()
        {
            Log.Logger.Information(
                "Automatically stopped recording having reached the {Limit} min limit",
                _optionsService.Options.MaxRecordingTimeMins);

            StopRecordingCommand.Execute(null);
        }

        private void AudioStopRequestedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Stop requested");
            RecordingStatus = RecordingStatus.StopRequested;
            RefreshRelayCommands();
        }

        private void AudioStoppedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Stopped recording");
            RecordingStatus = RecordingStatus.NotRecording;
            VolumeLevelAsPercentage = 0;
            _stopwatch.Stop();
            RefreshRelayCommands();
        }

        private void AudioStartedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Started recording");
            RecordingStatus = RecordingStatus.Recording;
            RefreshRelayCommands();
        }

        /// <summary>
        /// Recording status
        /// </summary>
        public RecordingStatus RecordingStatus
        {
            get => _recordingStatus;
            set
            {
                if (_recordingStatus != value)
                {
                    _recordingStatus = value;
                    StatusStr = value.GetDescriptiveText();

                    RaisePropertyChanged(nameof(RecordingStatus));
                    RaisePropertyChanged(nameof(IsNotRecording));
                    RaisePropertyChanged(nameof(IsRecordingOrStopping));
                }
            }
        }

        /// <summary>
        /// RecordingStatus as a string
        /// </summary>
        public string StatusStr
        {
            get => _statusStr;
            set
            {
                if (_statusStr != value)
                {
                    _statusStr = value;
                    RaisePropertyChanged(nameof(StatusStr));
                }
            }
        }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMsg
        {
            get => _errorMsg;
            set
            {
                if (_errorMsg != value)
                {
                    _errorMsg = value;
                    RaisePropertyChanged(nameof(ErrorMsg));
                }
            }
        }

        private void StartRecording()
        {
            try
            {
                ClearErrorMsg();
                Log.Logger.Information("Start requested");

                DateTime recordingDate = DateTime.Today;
                var candidateFile = _destinationService.GetRecordingFileCandidate(_optionsService, recordingDate, _commandLineIdentifier);

                CheckDiskSpace(candidateFile);

                _audioService.StartRecording(candidateFile, _optionsService);
                _stopwatch.Restart();
            }
            catch (Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_START;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

        private TimeSpan ElapsedTime
        {
            get
            {
                if (_stopwatch != null && _stopwatch.IsRunning)
                {
                    return _stopwatch.Elapsed;
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Elapsed recording time as string
        /// </summary>
        public string ElapsedTimeStr => ElapsedTime.ToString("hh\\:mm\\:ss");

        private void CheckDiskSpace(RecordingCandidate candidate)
        {
            if (candidate != null)
            {
                CheckDiskSpace(candidate.TempPath);
                CheckDiskSpace(candidate.FinalPath);
            }
        }

        private void CheckDiskSpace(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (FileUtils.DriveFreeBytes(Path.GetDirectoryName(filePath), out ulong bytesFree))
                {
                    if (bytesFree < _safeMinBytesFree)
                    {
                        // "Insufficient free space to record"
                        throw new Exception(Properties.Resources.INSUFFICIENT_FREE_SPACE);
                    }
                }
            }
        }

        private void StopRecording()
        {
            try
            {
                ClearErrorMsg();
                _audioService.StopRecording(_optionsService.Options.FadeOut);
            }
            catch (Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_STOP;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

        private void RefreshRelayCommands()
        {
            StartRecordingCommand.RaiseCanExecuteChanged();
            StopRecordingCommand.RaiseCanExecuteChanged();
            NavigateSettingsCommand.RaiseCanExecuteChanged();
        }

        private void ClearErrorMsg()
        {
            ErrorMsg = null;
        }

        private int _volumeLevel;

        public int VolumeLevelAsPercentage
        {
            get => _volumeLevel;
            set
            {
                if (_volumeLevel != value)
                {
                    _volumeLevel = value;
                    RaisePropertyChanged(nameof(VolumeLevelAsPercentage));
                }
            }
        }

        /// <summary>
        /// status == NotRecording?
        /// </summary>
        public bool IsNotRecording => RecordingStatus == RecordingStatus.NotRecording;

        public bool IsRecording => RecordingStatus == RecordingStatus.Recording;

        /// <summary>
        /// status == Recording or Stopping
        /// </summary>
        public bool IsRecordingOrStopping => RecordingStatus == RecordingStatus.Recording ||
                                             RecordingStatus == RecordingStatus.StopRequested;

        // Commands (bound in ctor)...
        public RelayCommand StartRecordingCommand { get; set; }

        public RelayCommand StopRecordingCommand { get; set; }

        public RelayCommand NavigateSettingsCommand { get; set; }
        
        /// <summary>
        /// Show brief animation
        /// </summary>
        private void DoSplash()
        {
            // "Splash" is a graphical effect rendered in the volume meter
            // when the Recording page loads for the first time. It's designed
            // to show that all is working, and OnlyR is ready to record!
            if (_splashTimer == null)
            {
                _splashTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(25) };
                _splashTimer.Tick += SplashTimerTick;
            }

            VolumeLevelAsPercentage = 100;
            _splashTimer.Start();
        }

        private void SplashTimerTick(object sender, EventArgs e)
        {
            if (IsNotRecording)
            {
                VolumeLevelAsPercentage -= 6;
                if (VolumeLevelAsPercentage <= 0)
                {
                    _splashTimer.Stop();
                }
            }
            else
            {
                _splashTimer.Stop();
            }
        }
    }
}
