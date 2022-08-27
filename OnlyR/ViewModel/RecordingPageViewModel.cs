using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using OnlyR.Core.Enums;
using OnlyR.Exceptions;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingCopies;
using OnlyR.Services.RecordingDestination;
using OnlyR.Services.Snackbar;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;
using Serilog;
using System.Globalization;

namespace OnlyR.ViewModel
{
    /// <summary>
    /// View model for Recording page. Contains properties that the Recording page 
    /// can data bind to, i.e. it has everything that is needed by the user during 
    /// interaction with the Recording page.
    /// </summary>
    public class RecordingPageViewModel : ObservableObject, IPage
    {
        private readonly IAudioService _audioService;
        private readonly IRecordingDestinationService _destinationService;
        private readonly IOptionsService _optionsService;
        private readonly ICopyRecordingsService _copyRecordingsService;
        private readonly ICommandLineService _commandLineService;
        private readonly ISnackbarService _snackbarService;
        private readonly ISilenceService _silenceService;
        private readonly ulong _safeMinBytesFree = 0x20000000;  // 0.5GB
        private readonly Stopwatch _stopwatch;
        private readonly ConcurrentDictionary<char, DateTime> _removableDrives = new();
        private DispatcherTimer? _splashTimer;
        private int _volumeLevel;
        private bool _isCopying;
        private RecordingStatus _recordingStatus;
        private string _statusStr;
        private string? _errorMsg;

        public RecordingPageViewModel(
            IAudioService audioService,
            IOptionsService optionsService,
            ICommandLineService commandLineService,
            IRecordingDestinationService destinationService,
            ICopyRecordingsService copyRecordingsService,
            ISnackbarService snackbarService,
            ISilenceService silenceService)
        {
            WeakReferenceMessenger.Default.Register<BeforeShutDownMessage>(this, OnShutDown);
            WeakReferenceMessenger.Default.Register<SessionEndingMessage>(this, OnSessionEnding);
            WeakReferenceMessenger.Default.Register<NavigateMessage>(this, OnNavigate);

            _commandLineService = commandLineService;
            _copyRecordingsService = copyRecordingsService;
            _snackbarService = snackbarService;
            _silenceService = silenceService;

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

            WeakReferenceMessenger.Default.Register<RemovableDriveMessage>(this, OnRemovableDriveMessage);
        }

        private void OnNavigate(object recipient, NavigateMessage message)
        {
            if (message.OriginalPageName == SettingsPageViewModel.PageName 
                && message.TargetPageName == PageName)
            {
                OnPropertyChanged(nameof(MaxRecordingTimeString));
                OnPropertyChanged(nameof(IsMaxRecordingTimeSpecified));
            }
        }

        public static string PageName => "RecordingPage";

        // Commands (bound in ctor)...
        public RelayCommand StartRecordingCommand { get; }

        public RelayCommand StopRecordingCommand { get; }

        public RelayCommand NavigateSettingsCommand { get; }

        public RelayCommand ShowRecordingsCommand { get; }

        public RelayCommand SaveToRemovableDriveCommand { get; }

        public bool IsCopying
        {
            get => _isCopying;
            set
            {
                if (_isCopying != value)
                {
                    _isCopying = value;
                    OnPropertyChanged(nameof(IsCopying));
                    OnPropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }

        public bool IsRecordingOrStopping => RecordingStatus == RecordingStatus.Recording ||
                                             RecordingStatus == RecordingStatus.StopRequested;

        public bool IsNotRecording => RecordingStatus == RecordingStatus.NotRecording;

        public bool IsRecording => RecordingStatus == RecordingStatus.Recording;

        public bool IsReadyToRecord => RecordingStatus != RecordingStatus.Recording &&
                                       RecordingStatus != RecordingStatus.StopRequested;

        public int VolumeLevelAsPercentage
        {
            get => _volumeLevel;
            set
            {
                if (_volumeLevel != value)
                {
                    _volumeLevel = value;
                    OnPropertyChanged(nameof(VolumeLevelAsPercentage));
                }
            }
        }

        public string? MaxRecordingTimeString =>
            _optionsService.Options.MaxRecordingTimeSeconds == 0
            ? null
            : TimeSpan.FromSeconds(_optionsService.Options.MaxRecordingTimeSeconds).ToString("hh\\:mm\\:ss", CultureInfo.CurrentCulture);

        public bool IsMaxRecordingTimeSpecified => _optionsService.Options.MaxRecordingTimeSeconds > 0;

        public string ElapsedTimeStr => ElapsedTime.ToString("hh\\:mm\\:ss", CultureInfo.CurrentCulture);

        public bool NoSettings => _commandLineService.NoSettings;

        public bool NoFolder => _commandLineService.NoFolder;

        public bool NoSave => _commandLineService.NoSave;

        public bool IsSaveVisible => !NoSave && !_removableDrives.IsEmpty;

        public bool IsSaveEnabled => !IsCopying && !IsRecordingOrStopping;

        /// <summary>
        /// Gets or sets the Recording status.
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

                    OnPropertyChanged(nameof(RecordingStatus));
                    OnPropertyChanged(nameof(IsNotRecording));
                    OnPropertyChanged(nameof(IsRecording));
                    OnPropertyChanged(nameof(IsReadyToRecord));
                    OnPropertyChanged(nameof(IsRecordingOrStopping));
                    OnPropertyChanged(nameof(IsSaveEnabled));
                }
            }
        }

        /// <summary>
        /// Gets or sets the Recording Status as a string.
        /// </summary>
        public string StatusStr
        {
            get => _statusStr;
            set
            {
                if (_statusStr != value)
                {
                    _statusStr = value;
                    OnPropertyChanged(nameof(StatusStr));
                }
            }
        }

        public string? ErrorMsg
        {
            get => _errorMsg;
            set
            {
                if (_errorMsg != value)
                {
                    _errorMsg = value;
                    OnPropertyChanged(nameof(ErrorMsg));
                }
            }
        }

        public string SaveHint
        {
            get
            {
                var driveLetterList = string.Join(", ", _removableDrives.Keys);
                return string.IsNullOrEmpty(driveLetterList) ? string.Empty : $"{Properties.Resources.SAVE_TO_DRIVES} - {driveLetterList}";
            }
        }

        private TimeSpan ElapsedTime => 
            _stopwatch.IsRunning ? _stopwatch.Elapsed : TimeSpan.Zero;

        private bool StopOnSilenceEnabled => _optionsService.Options.MaxSilenceTimeSeconds > 0;

        /// <summary>
        /// Responds to activation.
        /// </summary>
        /// <param name="state">RecordingPageNavigationState object (or null).</param>
        public void Activated(object? state)
        {
            // on display of page...
            var stateObj = (RecordingPageNavigationState?)state;
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

        private void NavigateSettings()
        {
            WeakReferenceMessenger.Default.Send(new NavigateMessage(PageName, SettingsPageViewModel.PageName, null));
        }

        private void OnShutDown(object recipient, BeforeShutDownMessage message)
        {
            // nothing to do
        }

        private void OnSessionEnding(object recipient, SessionEndingMessage e)
        {
            // allow the session to shutdown if we're not recording
            e.SessionEndingArgs.Cancel = RecordingStatus != RecordingStatus.NotRecording;
        }

        private void AudioProgressHandler(object? sender, Core.EventArgs.RecordingProgressEventArgs e)
        {
            VolumeLevelAsPercentage = e.VolumeLevelAsPercentage;
            OnPropertyChanged(nameof(ElapsedTimeStr));

            if (RecordingStatus != RecordingStatus.StopRequested)
            {
                if (_optionsService.Options.MaxRecordingTimeSeconds > 0 &&
                    ElapsedTime.TotalSeconds > _optionsService.Options.MaxRecordingTimeSeconds)
                {
                    AutoStopRecordingAtLimit();
                }

                if (StopOnSilenceEnabled)
                {
                    _silenceService.ReportVolume(e.VolumeLevelAsPercentage);

                    if (_silenceService.GetSecondsOfSilence() > _optionsService.Options.MaxSilenceTimeSeconds)
                    {
                        AutoStopRecordingAfterSilence();
                    }
                }
            }
        }
        
        private void AutoStopRecordingAfterSilence()
        {
            Log.Logger.Information(
                "Automatically stopped recording after {Limit} seconds of silence",
                _optionsService.Options.MaxSilenceTimeSeconds);

            StopRecordingCommand.Execute(null);
        }

        private void AutoStopRecordingAtLimit()
        {
            Log.Logger.Information(
                "Automatically stopped recording having reached the {Limit} second limit",
                _optionsService.Options.MaxRecordingTimeSeconds);

            StopRecordingCommand.Execute(null);
        }

        private void AudioStopRequestedHandler(object? sender, EventArgs e)
        {
            Log.Logger.Information("Stop requested");
            RecordingStatus = RecordingStatus.StopRequested;
        }

        private void AudioStoppedHandler(object? sender, EventArgs e)
        {
            Log.Logger.Information("Stopped recording");
            RecordingStatus = RecordingStatus.NotRecording;
            VolumeLevelAsPercentage = 0;
            _stopwatch.Stop();
        }

        private void AudioStartedHandler(object? sender, EventArgs e)
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

                _silenceService.Reset();

                var recordingDate = DateTime.Today;
                var candidateFile = _destinationService.GetRecordingFileCandidate(
                    _optionsService, recordingDate, _commandLineService.OptionsIdentifier);
                
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

        private void CheckDiskSpace(RecordingCandidate candidate)
        {
            CheckDiskSpace(candidate.TempPath);
            CheckDiskSpace(candidate.FinalPath);
        }

        private void CheckDiskSpace(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var folder = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            if (FileUtils.DriveFreeBytes(folder, out ulong bytesFree) && 
                bytesFree < _safeMinBytesFree)
            {
                // "Insufficient free space to record"
                throw new IOException(Properties.Resources.INSUFFICIENT_FREE_SPACE);
            }
        }

        private void StopRecording()
        {
            try
            {
                ClearErrorMsg();
                _audioService.StopRecording(_optionsService.Options?.FadeOut ?? false);
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

        private void SplashTimerTick(object? sender, EventArgs e)
        {
            if (IsNotRecording)
            {
                VolumeLevelAsPercentage -= 6;
                if (VolumeLevelAsPercentage <= 0)
                {
                    _splashTimer?.Stop();
                }
            }
            else
            {
                _splashTimer?.Stop();
            }
        }

        private void ShowRecordings()
        {
            var folder = FindSuitableRecordingFolderToShow();
            var psi = new ProcessStartInfo
            {
                FileName = folder, 
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        private string FindSuitableRecordingFolderToShow()
        {
            return FileUtils.FindSuitableRecordingFolderToShow(
                _commandLineService.OptionsIdentifier,
                _optionsService.Options?.DestinationFolder);
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
                        _ => { },
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
                    Application.Current.Dispatcher.Invoke(() => IsCopying = false);
                }
            });
        }

        private void OnRemovableDriveMessage(object recipient, RemovableDriveMessage message)
        {
            if (message.Added)
            {
                _removableDrives[message.DriveLetter] = DateTime.UtcNow;
            }
            else
            {
                _removableDrives.TryRemove(message.DriveLetter, out var _);
            }

            OnPropertyChanged(nameof(IsSaveVisible));
            OnPropertyChanged(nameof(SaveHint));
        }
    }
}
