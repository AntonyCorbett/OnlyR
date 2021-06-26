using Microsoft.Toolkit.Mvvm.Messaging;

namespace OnlyR.ViewModel
{
    using Microsoft.Toolkit.Mvvm.ComponentModel;
    using Microsoft.Toolkit.Mvvm.Input;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Messages;
    using Microsoft.WindowsAPICodePack.Dialogs;
    using Model;
    using Services.Audio;
    using Services.Options;
    using Utils;

    /// <summary>
    /// View model for Settings page. Contains properties that the Settings page 
    /// can data bind to, i.e. it has everything that is needed by the user during 
    /// interaction with the Settings page
    /// </summary>
    public class SettingsPageViewModel : ObservableObject, IPage
    {
        private readonly IOptionsService _optionsService;
        private readonly RecordingDeviceItem[] _recordingDevices;
        private readonly SampleRateItem[] _sampleRates;
        private readonly ChannelItem[] _channels;
        private readonly BitRateItem[] _bitRates;
        private readonly MaxRecordingTimeItem[] _maxRecordingTimes;
        private readonly RecordingLifeTimeItem[] _recordingLifetimes;
        private readonly ICommandLineService _commandLineService;
        private readonly LanguageItem[] _languages;
        private readonly MaxSilenceTimeItem[] _maxSilenceTimes;

        public SettingsPageViewModel(
            IAudioService audioService, 
            IOptionsService optionsService, 
            ICommandLineService commandLineService)
        {
            WeakReferenceMessenger.Default.Register<BeforeShutDownMessage>(this, OnShutDown);
            _optionsService = optionsService;

            _commandLineService = commandLineService;

            _recordingDevices = audioService.GetRecordingDeviceList();
            _sampleRates = optionsService.GetSupportedSampleRates();
            _channels = optionsService.GetSupportedChannels();
            _bitRates = optionsService.GetSupportedMp3BitRates();
            _maxRecordingTimes = GenerateMaxRecordingTimeItems();
            _recordingLifetimes = GenerateRecordingLifeTimes();
            _languages = GetSupportedLanguages();
            _maxSilenceTimes = GetMaxSilenceTimes();

            NavigateRecordingCommand = new RelayCommand(NavigateRecording);
            ShowRecordingsCommand = new RelayCommand(ShowRecordings);
            SelectDestinationFolderCommand = new RelayCommand(SelectDestinationFolder);
        }

        public static string PageName => "SettingsPage";

        // Commands (bound in ctor)...
        public RelayCommand NavigateRecordingCommand { get; set; }

        public RelayCommand ShowRecordingsCommand { get; set; }

        public RelayCommand SelectDestinationFolderCommand { get; set; }

        public IEnumerable<MaxRecordingTimeItem> MaxRecordingTimes => _maxRecordingTimes;

        public IEnumerable<RecordingLifeTimeItem> RecordingLifeTimes => _recordingLifetimes;

        public IEnumerable<MaxSilenceTimeItem> MaxSilenceTimeItems => _maxSilenceTimes;

        public int MaxSilenceTimeSeconds
        {
            get => _optionsService.Options.MaxSilenceTimeSeconds;
            set
            {
                if (_optionsService.Options.MaxSilenceTimeSeconds != value)
                {
                    _optionsService.Options.MaxSilenceTimeSeconds = value;
                }
            }
        }

        public int SilenceAsVolumePercentage
        {
            get => _optionsService.Options.SilenceAsVolumePercentage;
            set
            {
                if (_optionsService.Options.SilenceAsVolumePercentage != value)
                {
                    _optionsService.Options.SilenceAsVolumePercentage = value;
                }
            }
        }

        public int MaxRecordingTime
        {
            get => _optionsService.Options.MaxRecordingTimeSeconds;
            set
            {
                if (_optionsService.Options.MaxRecordingTimeSeconds != value)
                {
                    _optionsService.Options.MaxRecordingTimeSeconds = value;
                }
            }
        }

        public IEnumerable<LanguageItem> Languages => _languages;

        public string LanguageId
        {
            get => _optionsService.Culture;
            set
            {
                if (_optionsService.Culture != value)
                {
                    _optionsService.Culture = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RecordingLifeTime
        {
            get => _optionsService.Options.RecordingsLifeTimeDays;
            set
            {
                if (_optionsService.Options.RecordingsLifeTimeDays != value)
                {
                    _optionsService.Options.RecordingsLifeTimeDays = value;
                }
            }
        }

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
                    WeakReferenceMessenger.Default.Send(new AlwaysOnTopChanged());
                }
            }
        }

        public bool StartMinimized
        {
            get => _optionsService.Options.StartMinimized;
            set
            {
                if (_optionsService.Options.StartMinimized != value)
                {
                    _optionsService.Options.StartMinimized = value;
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

        public string DestinationFolder
        {
            get => _optionsService.Options.DestinationFolder;
            set
            {
                if (_optionsService.Options.DestinationFolder != value)
                {
                    _optionsService.Options.DestinationFolder = value;
                    OnPropertyChanged(nameof(DestinationFolder));
                }
            }
        }

        public string AppVersionStr => string.Format(Properties.Resources.APP_VER, GetVersionString());

        public IEnumerable<RecordingDeviceItem> RecordingDevices => _recordingDevices;

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

        public IEnumerable<SampleRateItem> SampleRates => _sampleRates;

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

        public IEnumerable<ChannelItem> Channels => _channels;

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

        public IEnumerable<BitRateItem> BitRates => _bitRates;

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

        public void Activated(object state)
        {
            // nothing to do
        }

        private static RecordingLifeTimeItem[] GenerateRecordingLifeTimes()
        {
            return new[]
            {
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_0, Days = 0 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_1_DAY, Days = 1 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_2_DAYS, Days = 2 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_1_WEEK, Days = 7 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_2_WEEKS, Days = 14 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_1_MONTH, Days = 31 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_2_MONTHS, Days = 62 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_6_MONTHS, Days = 365 / 2 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_1_YR, Days = 365 },
                new RecordingLifeTimeItem { Description = Properties.Resources.LIFE_2_YRS, Days = 365 * 2 },
            };
        }

        private static MaxSilenceTimeItem[] GetMaxSilenceTimes()
        {
            return new[]
            {
                new MaxSilenceTimeItem { Name = Properties.Resources.STOP_ON_SILENCE_DISABLED, Seconds = 0 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_SECS, 10), Seconds = 10 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_SECS, 15), Seconds = 15 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_SECS, 30), Seconds = 30 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_MINS, 1), Seconds = 60 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_MINS, 2), Seconds = 120 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_MINS, 3), Seconds = 180 },
                new MaxSilenceTimeItem { Name = string.Format(Properties.Resources.X_MINS, 5), Seconds = 300 },
            };
        }

        private static MaxRecordingTimeItem[] GenerateMaxRecordingTimeItems()
        {
            return new[]
            {
                new MaxRecordingTimeItem { Name = Properties.Resources.NO_LIMIT, ActualSeconds = 0 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_SECS, 15), ActualSeconds = 15 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_SECS, 30), ActualSeconds = 30 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_SECS, 45), ActualSeconds = 45 },
                new MaxRecordingTimeItem { Name = Properties.Resources.ONE_MIN, ActualSeconds = 1 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_MINS, 2), ActualSeconds = 2 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_MINS, 5), ActualSeconds = 5 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_MINS, 15), ActualSeconds = 15 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_MINS, 30), ActualSeconds = 30 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_MINS, 45), ActualSeconds = 45 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.ONE_HOUR, 1), ActualSeconds = 60 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_HOURS, 2), ActualSeconds = 120 * 60 },
                new MaxRecordingTimeItem { Name = string.Format(Properties.Resources.X_HOURS, 3), ActualSeconds = 180 * 60 },
            };
        }
        
        private void NavigateRecording()
        {
            Save();

            WeakReferenceMessenger.Default.Send(new NavigateMessage(
                SettingsPageViewModel.PageName,
                RecordingPageViewModel.PageName, 
                null));
        }

        private void Save()
        {
            _optionsService.Save();
        }

        private static string GetVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }

        private void OnShutDown(object recipient, BeforeShutDownMessage obj)
        {
            Save();
        }

        private void SelectDestinationFolder()
        {
#pragma warning disable CA1416 // Validate platform compatibility
            using var d = new CommonOpenFileDialog(Properties.Resources.SELECT_DEST_FOLDER) { IsFolderPicker = true };
            var result = d.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                DestinationFolder = d.FileName;
            }
#pragma warning restore CA1416 // Validate platform compatibility
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
                _optionsService.Options.DestinationFolder);
        }

        private static LanguageItem[] GetSupportedLanguages()
        {
            var result = new List<LanguageItem>();

            var subFolders = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory);

            foreach (var folder in subFolders)
            {
                if (!string.IsNullOrEmpty(folder))
                {
                    try
                    {
                        var c = new CultureInfo(Path.GetFileNameWithoutExtension(folder));
                        result.Add(new LanguageItem
                        {
                            LanguageId = c.Name,
                            LanguageName = c.EnglishName,
                        });
                    }
                    catch (CultureNotFoundException)
                    {
                        // expected
                    }
                }
            }

            // the native language
            var cNative = new CultureInfo(Path.GetFileNameWithoutExtension("en-GB"));
            result.Add(new LanguageItem
            {
                LanguageId = cNative.Name,
                LanguageName = cNative.EnglishName,
            });

            result.Sort((x, y) => string.Compare(x.LanguageName, y.LanguageName, StringComparison.Ordinal));

            return result.ToArray();
        }
    }
}
