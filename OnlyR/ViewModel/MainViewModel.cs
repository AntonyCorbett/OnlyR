using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OnlyR.Model;
using OnlyR.Services;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Services.RecordingDestination;
using OnlyR.Core.Enums;
using OnlyR.Utils;
using Serilog;

namespace OnlyR.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to, i.e. it has everything
    /// that is needed by the user during interaction.
    /// <para>
    /// See http://www.galasoft.ch/mvvm for details of the mvvm light framework
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private IAudioService _audioService;
        private IRecordingDestinationService _destinationService;
        private IOptionsService _optionsService;
        private string _commandLineIdentifier;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(
           IAudioService audioService,
           IOptionsService optionsService,
           IRecordingDestinationService destinationService)
        {
            _commandLineIdentifier = CommandLineParser.Instance.GetId();
            
            _audioService = audioService;
            _audioService.StartedEvent += AudioStartedHandler;
            _audioService.StoppedEvent += AudioStoppedHandler;
            _audioService.StopRequested += AudioStopRequestedHandler;
            
            _optionsService = optionsService;
            _destinationService = destinationService;
            _recordingStatus = RecordingStatus.NotRecording;

            _statusStr = "All ok";

            // bind commands...
            StartRecordingCommand = new RelayCommand(StartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording);
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
        }

        private void AudioStartedHandler(object sender, EventArgs e)
        {
            Log.Logger.Information("Started recording");
            RecordingStatus = RecordingStatus.Recording;
        }

        private RecordingStatus _recordingStatus;
        public RecordingStatus RecordingStatus
        {
            get { return _recordingStatus; }
            set
            {
                if (_recordingStatus != value)
                {
                    _recordingStatus = value;
                    StatusStr = value.GetDescriptiveText();

                    RaisePropertyChanged(nameof(RecordingStatus));
                    RaisePropertyChanged(nameof(IsStartEnabled));
                    RaisePropertyChanged(nameof(IsStopEnabled));
                }
            }
        }

        private string _statusStr;
        public string StatusStr
        {
            get { return _statusStr; }
            set
            {
                if(_statusStr != value)
                {
                    _statusStr = value;
                    RaisePropertyChanged(nameof(StatusStr));
                }
            }
        }

        private string _errorMsg;
        public string ErrorMsg
        {
            get { return _errorMsg; }
            set
            {
                if(_errorMsg != value)
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
                _audioService.StartRecording(candidateFile);
            }
            catch(Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_START;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

        private void StopRecording()
        {
            try
            {
                ClearErrorMsg();
                _audioService.StopRecording();
            }
            catch(Exception ex)
            {
                ErrorMsg = Properties.Resources.ERROR_STOP;
                Log.Logger.Error(ex, ErrorMsg);
            }
        }

        private void ClearErrorMsg()
        {
            ErrorMsg = null;
        }

        private int _volumeLevel;
        public int VolumeLevelAsPercentage
        {
            get { return _volumeLevel; }
            set
            {
                if (_volumeLevel != value)
                {
                    _volumeLevel = value;
                    RaisePropertyChanged(nameof(VolumeLevelAsPercentage));
                }
            }
        }

        public bool IsStartEnabled => RecordingStatus == RecordingStatus.NotRecording;
        public bool IsStopEnabled => RecordingStatus == RecordingStatus.Recording;


        // Commands (bound in ctor)...
        public RelayCommand StartRecordingCommand { get; set; }
        public RelayCommand StopRecordingCommand { get; set; }
        //...
    }


}