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
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using OnlyR.Utils;
using OnlyR.ViewModel.Messages;
using Serilog;

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
            SelectDestinationFolderCommand = new RelayCommand(SelectDestinationFolder);
        }

        private void SelectDestinationFolder()
        {
            CommonOpenFileDialog d = new CommonOpenFileDialog(Properties.Resources.SELECT_DEST_FOLDER) { IsFolderPicker = true};
            CommonFileDialogResult result = d.ShowDialog();
            if(result == CommonFileDialogResult.Ok)
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
            string folder = null;

            try
            {
                DateTime today = DateTime.Today;
                string commandLineIdentifier = CommandLineParser.Instance.GetId();

                // first try today's folder...
                folder = FileUtils.GetDestinationFolder(today, commandLineIdentifier,
                    _optionsService.Options.DestinationFolder);
                if (!Directory.Exists(folder))
                {
                    // try this month's folder...
                    folder = FileUtils.GetMonthlyDestinationFolder(today, commandLineIdentifier,
                        _optionsService.Options.DestinationFolder);
                    if (!Directory.Exists(folder))
                    {
                        folder = FileUtils.GetRootDestinationFolder(commandLineIdentifier,
                            _optionsService.Options.DestinationFolder);
                        if (!Directory.Exists(folder) && !string.IsNullOrEmpty(commandLineIdentifier))
                        {
                            folder = FileUtils.GetRootDestinationFolder(string.Empty,
                                _optionsService.Options.DestinationFolder);

                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Logger.Error(ex, $"Could not find destination folder {folder}");
            }

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                folder = FileUtils.GetDefaultMyDocsDestinationFolder();
                Directory.CreateDirectory(folder);
            }

            return folder;
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
                }
            }
        }

        // Genre...
        public string Genre
        {
            get => _optionsService.Options.Genre;
            set
            {
                if(_optionsService.Options.Genre != value)
                {
                    _optionsService.Options.Genre = value;
                }
            }
        } 

        // Destination folder...
        public string DestinationFolder
        {
            get => _optionsService.Options.DestinationFolder;
            set
            {
                if(_optionsService.Options.DestinationFolder != value)
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

        public void Activated(object state)
        {
            
        }

        // Commands (bound in ctor)...
        public RelayCommand NavigateRecordingCommand { get; set; }
        public RelayCommand ShowRecordingsCommand { get; set; }
        public RelayCommand SelectDestinationFolderCommand { get; set; }
        //...

    }
}
