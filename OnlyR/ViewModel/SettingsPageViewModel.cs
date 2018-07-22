namespace OnlyR.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Messaging;
    using Messages;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Model;
    using Serilog;
    using Services.Audio;
    using Services.Options;
    using Utils;

    /// <summary>
    /// View model for Settings page. Contains properties that the Settings page 
    /// can data bind to, i.e. it has everything that is needed by the user during 
    /// interaction with the Settings page
    /// </summary>
    public class SettingsPageViewModel : ViewModelBase, IPage
    {
        public static string PageName => "SettingsPage";

        private readonly IOptionsService _optionsService;
        private readonly List<RecordingDeviceItem> _recordingDevices;
        private readonly List<SampleRateItem> _sampleRates;
        private readonly List<ChannelItem> _channels;
        private readonly List<BitRateItem> _bitRates;
        private readonly List<MaxRecordingTimeItem> _maxRecordingTimes;
        private readonly ICommandLineService _commandLineService;

        public SettingsPageViewModel(
            IAudioService audioService, 
            IOptionsService optionsService, 
            ICommandLineService commandLineService)
        {
            Messenger.Default.Register<BeforeShutDownMessage>(this, OnShutDown);
            _optionsService = optionsService;

            _commandLineService = commandLineService;

            _recordingDevices = audioService.GetRecordingDeviceList().ToList();
            _sampleRates = optionsService.GetSupportedSampleRates().ToList();
            _channels = optionsService.GetSupportedChannels().ToList();
            _bitRates = optionsService.GetSupportedMp3BitRates().ToList();
            _maxRecordingTimes = GenerateMaxRecordingTimeItems();

            NavigateRecordingCommand = new RelayCommand(NavigateRecording, CanExecuteNavigateRecording);
            ShowRecordingsCommand = new RelayCommand(ShowRecordings);
            SelectDestinationFolderCommand = new RelayCommand(SelectDestinationFolder);
        }

        public void Activated(object state)
        {
            // nothing to do
        }

        /// <summary>
        /// Application version number (see SolutionInfo.cs)
        /// </summary>
        public string AppVersionStr => string.Format(Properties.Resources.APP_VER, GetVersionString());

        /// <summary>
        /// Collection of Windows recording devices
        /// </summary>
        public IEnumerable<RecordingDeviceItem> RecordingDevices => _recordingDevices;

        /// <summary>
        /// Selected recording device Id
        /// </summary>
        public int RecordingDeviceId
        {
            get => _optionsService.Options.RecordingDevice;
            set
            {
                if (_optionsService.Options.RecordingDevice != value)
                {
                    _optionsService.Options.RecordingDevice = value;
                }
            }
        }

        /// <summary>
        /// Collection of possible sample rates
        /// </summary>
        public IEnumerable<SampleRateItem> SampleRates => _sampleRates;

        /// <summary>
        /// Selected sample rate
        /// </summary>
        public int SampleRate
        {
            get => _optionsService.Options.SampleRate;
            set
            {
                if (_optionsService.Options.SampleRate != value)
                {
                    _optionsService.Options.SampleRate = value;
                }
            }
        }

        /// <summary>
        /// Collection of valid channel count values
        /// </summary>
        public IEnumerable<ChannelItem> Channels => _channels;

        /// <summary>
        /// Selected channel count
        /// </summary>
        public int Channel
        {
            get => _optionsService.Options.ChannelCount;
            set
            {
                if (_optionsService.Options.ChannelCount != value)
                {
                    _optionsService.Options.ChannelCount = value;
                }
            }
        }

        /// <summary>
        /// Collection of supported MP3 encoding bit rates
        /// </summary>
        public IEnumerable<BitRateItem> BitRates => _bitRates;

        /// <summary>
        /// Selected MP3 bit rate
        /// </summary>
        public int BitRate
        {
            get => _optionsService.Options.Mp3BitRate;
            set
            {
                if (_optionsService.Options.Mp3BitRate != value)
                {
                    _optionsService.Options.Mp3BitRate = value;
                }
            }
        }

        private static List<MaxRecordingTimeItem> GenerateMaxRecordingTimeItems()
        {
            var result = new List<MaxRecordingTimeItem>();

            AddMaxRecordingItem(result, Properties.Resources.NO_LIMIT, 0);

            AddMaxRecordingItem(result, Properties.Resources.ONE_MIN, 1);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_MINS, 2), 2);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_MINS, 5), 5);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_MINS, 15), 15);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_MINS, 30), 30);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_MINS, 45), 45);

            AddMaxRecordingItem(result, string.Format(Properties.Resources.ONE_HOUR, 1), 60);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_HOURS, 2), 120);
            AddMaxRecordingItem(result, string.Format(Properties.Resources.X_HOURS, 3), 180);

            return result;
        }

        private static void AddMaxRecordingItem(ICollection<MaxRecordingTimeItem> result, string name, int timeInMins)
        {
            result.Add(new MaxRecordingTimeItem { Name = name, ActualMinutes = timeInMins });
        }

        /// <summary>
        /// Collection of supported "max recording" times
        /// </summary>
        public IEnumerable<MaxRecordingTimeItem> MaxRecordingTimes => _maxRecordingTimes;

        /// <summary>
        /// Selected max recording time
        /// </summary>
        public int MaxRecordingTime
        {
            get => _optionsService.Options.MaxRecordingTimeMins;
            set
            {
                if (_optionsService.Options.MaxRecordingTimeMins != value)
                {
                    _optionsService.Options.MaxRecordingTimeMins = value;
                }
            }
        }

        /// <summary>
        /// Whether the audio should be faded to zero when the user stops recording
        /// </summary>
        public bool ShouldFadeRecordings
        {
            get => _optionsService.Options.FadeOut;
            set
            {
                if (_optionsService.Options.FadeOut != value)
                {
                    _optionsService.Options.FadeOut = value;
                }
            }
        }

        /// <summary>
        /// Whether recording should begin on launch
        /// </summary>
        public bool StartRecordingOnLaunch
        {
            get => _optionsService.Options.StartRecordingOnLaunch;
            set
            {
                if (_optionsService.Options.StartRecordingOnLaunch != value)
                {
                    _optionsService.Options.StartRecordingOnLaunch = value;
                }
            }
        }

        public bool AlwaysOnTop
        {
            get => _optionsService.Options.AlwaysOnTop;
            set
            {
                if (_optionsService.Options.AlwaysOnTop != value)
                {
                    _optionsService.Options.AlwaysOnTop = value;
                    Messenger.Default.Send(new AlwaysOnTopChanged());
                }
            }
        }

        public bool AllowCloseWhenRecording
        {
            get => _optionsService.Options.AllowCloseWhenRecording;
            set
            {
                if (_optionsService.Options.AllowCloseWhenRecording != value)
                {
                    _optionsService.Options.AllowCloseWhenRecording = value;
                }
            }
        }
        
        /// <summary>
        /// The Genre of the recording. This is stored in the MP3 Id3 tag data 
        /// (i.e. within the MP3 file itself)
        /// </summary>
        public string Genre
        {
            get => _optionsService.Options.Genre;
            set
            {
                if (_optionsService.Options.Genre != value)
                {
                    _optionsService.Options.Genre = value;
                }
            }
        }

        /// <summary>
        /// The root destination folder for recordings
        /// </summary>
        public string DestinationFolder
        {
            get => _optionsService.Options.DestinationFolder;
            set
            {
                if (_optionsService.Options.DestinationFolder != value)
                {
                    _optionsService.Options.DestinationFolder = value;
                    RaisePropertyChanged(nameof(DestinationFolder));
                }
            }
        }

        private static bool CanExecuteNavigateRecording()
        {
            return true;
        }

        private void NavigateRecording()
        {
            Save();
            Messenger.Default.Send(new NavigateMessage(RecordingPageViewModel.PageName, null));
        }

        private void Save()
        {
            _optionsService.Save();
        }

        // Commands (bound in ctor)...
        public RelayCommand NavigateRecordingCommand { get; set; }

        public RelayCommand ShowRecordingsCommand { get; set; }

        public RelayCommand SelectDestinationFolderCommand { get; set; }


        private string GetVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }

        private void OnShutDown(BeforeShutDownMessage obj)
        {
            Save();
        }

        private void SelectDestinationFolder()
        {
            CommonOpenFileDialog d = new CommonOpenFileDialog(Properties.Resources.SELECT_DEST_FOLDER) { IsFolderPicker = true };
            CommonFileDialogResult result = d.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                DestinationFolder = d.FileName;
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
    }
}
