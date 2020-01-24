namespace OnlyR.ViewModel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Core.Enums;
    using Exceptions;
    using Fclp.Internals.Extensions;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using GalaSoft.MvvmLight.Messaging;
    using GalaSoft.MvvmLight.Threading;
    using Messages;
    using Model;
    using Serilog;
    using Services.Audio;
    using Services.Options;
    using Services.RecordingCopies;
    using Services.RecordingDestination;
    using Services.Snackbar;
    using Utils;

    /// <summary>
    /// View model for Recording page. Contains properties that the Recording page 
    /// can data bind to, i.e. it has everything that is needed by the user during 
    /// interaction with the Recording page
    /// </summary>
    public class RecordingPageViewModel : ViewModelBase, IPage
    {
        private static bool _stopRequested = false;

        private readonly IAudioService _audioService;
        private readonly IRecordingDestinationService _destinationService;
        private readonly IOptionsService _optionsService;
        private readonly ICopyRecordingsService _copyRecordingsService;
        private readonly ICommandLineService _commandLineService;
        private readonly ISnackbarService _snackbarService;
        private readonly ulong _safeMinBytesFree = 0x20000000;  // 0.5GB
        private readonly Stopwatch _stopwatch;
        private readonly ConcurrentDictionary<char, DateTime> _removableDrives = new ConcurrentDictionary<char, DateTime>();
        private readonly Queue<VolumeSample> _volumeAverage = new Queue<VolumeSample>();
        private readonly int _minVolumeAverage = 5;  // TODO: This could be moved to a config file to allow tweeking
        private DispatcherTimer _splashTimer;
        private int _volumeLevel;
        private bool _isCopying;
        private RecordingStatus _recordingStatus;
        private string _statusStr;
        private string _errorMsg;

        public RecordingPageViewModel(
            IAudioService audioService,
            IOptionsService optionsService,
            ICommandLineService commandLineService,
            IRecordingDestinationService destinationService,
            ICopyRecordingsService copyRecordingsService,
            ISnackbarService snackbarService)
        {
            Messenger.Default.Register<BeforeShutDownMessage>(this, OnShutDown);

            _commandLineService = commandLineService;
            _copyRecordingsService = copyRecordingsService;
            _snackbarService = snackbarService;

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
            StartRecordingCommand = new RelayCommand(StartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording);
            NavigateSettingsCommand = new RelayCommand(NavigateSettings);
            ShowRecordingsCommand = new RelayCommand(ShowRecordings);
            SaveToRemovableDriveCommand = new RelayCommand(SaveToRemovableDrives);

            Messenger.Default.Register<RemovableDriveMessage>(this, OnRemovableDriveMessage);
        }

        public static string PageName => "RecordingPage";

        // Commands (bound in ctor)...
        public RelayCommand StartRecordingCommand { get; set; }

        public RelayCommand StopRecordingCommand { get; set; }

        public RelayCommand NavigateSettingsCommand { get; set; }

        public RelayCommand ShowRecordingsCommand { get; set; }

        public RelayCommand SaveToRemovableDriveCommand { get; set; }

        public bool IsCopying
        {
            get => _isCopying;
            set
            {
                if (_isCopying != value)
                {
                    _isCopying = value;
                    RaisePropertyChanged(nameof(IsCopying));
                    RaisePropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }

        public bool IsRecordingOrStopping => RecordingStatus == RecordingStatus.Recording ||
                                             RecordingStatus == RecordingStatus.StopRequested;

        public bool IsNotRecording => RecordingStatus == RecordingStatus.NotRecording;

        public bool IsRecording => RecordingStatus == RecordingStatus.Recording;

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

        public string ElapsedTimeStr => ElapsedTime.ToString("hh\\:mm\\:ss");

        public bool NoSettings => _commandLineService.NoSettings;

        public bool NoFolder => _commandLineService.NoFolder;

        public bool NoSave => _commandLineService.NoSave;

        public bool IsSaveVisible => !NoSave && _removableDrives.Any();

        public bool IsSaveEnabled => !IsCopying && !IsRecordingOrStopping;

        /// <summary>
        /// Gets or sets the Recording status
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
                    RaisePropertyChanged(nameof(IsRecording));
                    RaisePropertyChanged(nameof(IsRecordingOrStopping));
                    RaisePropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }

        /// <summary>
        /// Gets or sets the Recording Status as a string
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

        public string SaveHint
        {
            get
            {
                var driveLetterList = string.Join(", ", _removableDrives.Keys);

                if (string.IsNullOrEmpty(driveLetterList))
                {
                    return string.Empty;
                }

                if (driveLetterList.Contains(","))
                {
                    return string.Format(Properties.Resources.SAVE_TO_DRIVES, driveLetterList);
                }

                return string.Format(Properties.Resources.SAVE_TO_DRIVE, driveLetterList);
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
            Messenger.Default.Send(new NavigateMessage(
                RecordingPageViewModel.PageName,
                SettingsPageViewModel.PageName,
                null));
        }

        private void OnShutDown(BeforeShutDownMessage message)
        {
            // nothing to do
        }

        private void AudioProgressHandler(object sender, Core.EventArgs.RecordingProgressEventArgs e)
        {
            if (!_stopRequested)
            {
                VolumeLevelAsPercentage = e.VolumeLevelAsPercentage;
                RaisePropertyChanged(nameof(ElapsedTimeStr));

                DateTime currentTime = DateTime.Now;

                if (_optionsService.Options.StopOnSilence)
                {
                    SaveVolumeAverage(currentTime);
                }

                if (_optionsService.Options.MaxRecordingTimeMins > 0 &&
                    ElapsedTime.TotalSeconds > _optionsService.Options.MaxRecordingTimeMins * 60)
                {
                    AutoStopRecording();
                }

                if (_optionsService.Options.StopOnSilence)
                {
                    StopIfSilenceDetected(currentTime);
                }
            }
        }

        private void StopIfSilenceDetected(DateTime timeStamp)
        {
            int average = 0;
            _volumeAverage.ForEach(v => average += v.VolumeLevel);
            average /= _volumeAverage.Count;

            // Only check average if we have at least SilenceSeconds of volume data
            if (_volumeAverage.Peek().Timestamp < timeStamp.AddSeconds(_optionsService.Options.SilencePeriod * -1) && average < _minVolumeAverage)
            {
                _stopRequested = true;  // Prevent threading from requesting multiple stops
                AutoStopRecording();
            }
        }

        private void SaveVolumeAverage(DateTime currentTime)
        {
            // Only keep last SilenceSeconds + 1 seconds for average calculation
            while (_volumeAverage.Count > 0 && _volumeAverage.Peek().Timestamp < currentTime.AddSeconds((_optionsService.Options.SilencePeriod + 1) * -1))
            {
                _ = _volumeAverage.Dequeue();
            }

            _volumeAverage.Enqueue(new VolumeSample { VolumeLevel = VolumeLevelAsPercentage, Timestamp = currentTime });
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
        }

        private void AudioStoppedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Stopped recording");
            RecordingStatus = RecordingStatus.NotRecording;
            VolumeLevelAsPercentage = 0;
            _stopwatch.Stop();
        }

        private void AudioStartedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Started recording");
            RecordingStatus = RecordingStatus.Recording;
        }

        private void StartRecording()
        {
            try
            {
                ClearErrorMsg();
                Log.Logger.Information("Start requested");

                DateTime recordingDate = DateTime.Today;
                var candidateFile = _destinationService.GetRecordingFileCandidate(
                    _optionsService, recordingDate, _commandLineService.OptionsIdentifier);

                CheckDiskSpace(candidateFile);

                _stopRequested = false;
                _audioService.StartRecording(candidateFile, _optionsService);
                _stopwatch.Restart();
            }
            catch (Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_START;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

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
                _volumeAverage.Clear();
                _audioService.StopRecording(_optionsService.Options.FadeOut);
            }
            catch (Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_STOP;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

        private void ClearErrorMsg()
        {
            ErrorMsg = null;
        }

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

        private void ShowRecordings()
        {
            Process.Start(FindSuitableRecordingFolderToShow());
        }

        private string FindSuitableRecordingFolderToShow()
        {
            return FileUtils.FindSuitableRecordingFolderToShow(
                _commandLineService.OptionsIdentifier,
                _optionsService.Options.DestinationFolder);
        }

        private void SaveToRemovableDrives()
        {
            IsCopying = true;

            Task.Run(() =>
            {
                try
                {
                    _copyRecordingsService.Copy(_removableDrives.Keys.ToArray());
                    _snackbarService.Enqueue(
                        Properties.Resources.COPIED,
                        Properties.Resources.OK,
                        o => { },
                        null,
                        promote: false,
                        neverConsiderToBeDuplicate: true);
                }
                catch (NoRecordingsException ex)
                {
                    _snackbarService.EnqueueWithOk(ex.Message);
                }
                catch (AggregateException ex)
                {
                    foreach (var innerException in ex.InnerExceptions)
                    {
                        if (innerException is NoSpaceException exception)
                        {
                            _snackbarService.EnqueueWithOk(exception.Message);
                        }
                        else
                        {
                            _snackbarService.EnqueueWithOk(Properties.Resources.UNKNOWN_COPY_ERROR);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, Properties.Resources.UNKNOWN_COPY_ERROR);
                    _snackbarService.EnqueueWithOk(Properties.Resources.UNKNOWN_COPY_ERROR);
                }
                finally
                {
                    DispatcherHelper.CheckBeginInvokeOnUI(() => { IsCopying = false; });
                }
            });
        }

        private void OnRemovableDriveMessage(RemovableDriveMessage messsage)
        {
            if (messsage.Added)
            {
                _removableDrives[messsage.DriveLetter] = DateTime.UtcNow;
            }
            else
            {
                _removableDrives.TryRemove(messsage.DriveLetter, out var _);
            }

            RaisePropertyChanged(nameof(IsSaveVisible));
            RaisePropertyChanged(nameof(SaveHint));
        }
    }
}
