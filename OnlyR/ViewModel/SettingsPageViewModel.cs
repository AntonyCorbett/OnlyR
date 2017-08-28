using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;

namespace OnlyR.ViewModel
{
    public class SettingsPageViewModel : ViewModelBase, IPage
    {
        public static string PageName => "SettingsPage";

        private readonly IOptionsService _optionsService;
        
        public SettingsPageViewModel(IAudioService audioService, IOptionsService optionsService)
        {
            _optionsService = optionsService;

            _recordingDevices = audioService.GetRecordingDeviceList().ToList();
            _sampleRates = optionsService.GetSupportedSampleRates().ToList();
            _channels = optionsService.GetSupportedChannels().ToList();
            _bitRates = optionsService.GetSupportedMp3BitRates().ToList();
            _maxRecordingTimes = GenerateMaxRecordingTimeItems();

            NavigateRecordingCommand = new RelayCommand(NavigateRecording, CanExecuteNavigateRecording);
            ShowRecordingsCommand = new RelayCommand(ShowRecordings);
        }

        private static void ShowRecordings()
        {
            DateTime today = DateTime.Today;
            string commandLineIdentifier = CommandLineParser.Instance.GetId();

            // first try today's folder...
            var folder = FileUtils.GetDestinationFolder(today, commandLineIdentifier);
            if(!Directory.Exists(folder))
            {
                // try this month's folder...
                folder = FileUtils.GetMonthlyDestinationFolder(today, commandLineIdentifier);
                if (!Directory.Exists(folder))
                {
                    folder = FileUtils.GetRootDestinationFolder(commandLineIdentifier);
                    if (!Directory.Exists(folder) && !string.IsNullOrEmpty(commandLineIdentifier))
                    {
                        folder = FileUtils.GetRootDestinationFolder(string.Empty);
                    }
                }
            }
            
            Process.Start(folder);
        }

        public string AppVersionStr => string.Format(Properties.Resources.APP_VER, GetVersionString());

        private string GetVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }

        // Recording Device...
        private readonly List<RecordingDeviceItem> _recordingDevices;
        public IEnumerable<RecordingDeviceItem> RecordingDevices => _recordingDevices;
        
        public int RecordingDeviceId
        {
            get => _optionsService.Options.RecordingDevice;
            set
            {
                if (_optionsService.Options.RecordingDevice != value)
                {
                    _optionsService.Options.RecordingDevice = value;
                    _optionsService.Save();
                }
            }
        }

        // Sample rates...
        private readonly List<SampleRateItem> _sampleRates;

        public IEnumerable<SampleRateItem> SampleRates => _sampleRates;

        public int SampleRate
        {
            get => _optionsService.Options.SampleRate;
            set
            {
                if(_optionsService.Options.SampleRate != value)
                {
                    _optionsService.Options.SampleRate = value;
                    _optionsService.Save();
                }
            }
        }

        // Channels...
        private readonly List<ChannelItem> _channels;
        public IEnumerable<ChannelItem> Channels => _channels;

        public int Channel
        {
            get => _optionsService.Options.ChannelCount;
            set
            {
                if(_optionsService.Options.ChannelCount != value)
                {
                    _optionsService.Options.ChannelCount = value;
                    _optionsService.Save();
                }
            }
        }

        // MP3 bit-rates...
        private readonly List<BitRateItem> _bitRates;
        public IEnumerable<BitRateItem> BitRates => _bitRates;

        public int BitRate
        {
            get => _optionsService.Options.Mp3BitRate;
            set
            {
                if (_optionsService.Options.Mp3BitRate != value)
                {
                    _optionsService.Options.Mp3BitRate = value;
                    _optionsService.Save();
                }
            }
        }

        // Max recording time...
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

        private readonly List<MaxRecordingTimeItem> _maxRecordingTimes;
        public IEnumerable<MaxRecordingTimeItem> MaxRecordingTimes => _maxRecordingTimes;

        public int MaxRecordingTime
        {
            get => _optionsService.Options.MaxRecordingTimeMins;
            set
            {
                if (_optionsService.Options.MaxRecordingTimeMins != value)
                {
                    _optionsService.Options.MaxRecordingTimeMins = value;
                    _optionsService.Save();
                }
            }
        }

        // Fade...
        public bool ShouldFadeRecordings
        {
            get => _optionsService.Options.FadeOut;
            set
            {
                if(_optionsService.Options.FadeOut != value)
                {
                    _optionsService.Options.FadeOut = value;
                    _optionsService.Save();
                }
            }
        }

        // Genre...
        public string Genre {
            get => _optionsService.Options.Genre;
            set
            {
                if(_optionsService.Options.Genre != value)
                {
                    _optionsService.Options.Genre = value;
                    _optionsService.Save();
                }
            }
        } 


        private static bool CanExecuteNavigateRecording()
        {
            return true;
        }

        private static void NavigateRecording()
        {
            Messenger.Default.Send(new NavigateMessage(RecordingPageViewModel.PageName, null));
        }

        public void Activated(object state)
        {
            
        }

        // Commands (bound in ctor)...
        public RelayCommand NavigateRecordingCommand { get; set; }
        public RelayCommand ShowRecordingsCommand { get; set; }
        //...

    }
}
