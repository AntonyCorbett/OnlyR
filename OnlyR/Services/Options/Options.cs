using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OnlyR.Services.Options
{
    public class Options
    {
        private static readonly int _defaultMaxRecordings = 100;

        private static readonly int[] _validSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000 };
        private static readonly int _defaultSampleRate = 44100;

        private static readonly int[] _validChannelCounts = { 1, 2 };
        private static readonly int _defaultChannelCount = 1;

        private static readonly int[] _validMp3BitRates = { 320, 256, 224, 192, 160, 144, 128, 112, 96, 80, 64, 56, 48, 32 };
        private static readonly int _defaultMp3BitRate = 96;

        private static readonly int _defaultRecordingDevice = 0;
        private static readonly int _defaultMaxRecordingMins = 0;  // no limit

        public int MaxRecordingsInOneFolder { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public int Mp3BitRate { get; set; }
        public string Genre { get; set; }
        public int MaxRecordingTimeMins { get; set; }
        public int RecordingDevice { get; set; }
        public bool FadeOut { get; set; }

        public Options()
        {
            MaxRecordingsInOneFolder = _defaultMaxRecordings;
            SampleRate = _defaultSampleRate;
            ChannelCount = _defaultChannelCount;
            Mp3BitRate = _defaultMp3BitRate;
            Genre = Properties.Resources.SPEECH;
            MaxRecordingTimeMins = _defaultMaxRecordingMins;
            RecordingDevice = _defaultRecordingDevice;
        }

        public static IEnumerable<int> GetSupportedSampleRates()
        {
            return _validSampleRates;
        }

        public static IEnumerable<int> GetSupportedChannels()
        {
            return _validChannelCounts;
        }

        public static IEnumerable<int> GetSupportedMp3BitRates()
        {
            return _validMp3BitRates;
        }


        public void Sanitize()
        {
            Debug.Assert(_validChannelCounts.Contains(_defaultChannelCount));
            Debug.Assert(_validSampleRates.Contains(_defaultSampleRate));
            Debug.Assert(_validMp3BitRates.Contains(_defaultMp3BitRate));

            if (MaxRecordingsInOneFolder < 10 || MaxRecordingsInOneFolder > 500)
            {
                MaxRecordingsInOneFolder = _defaultMaxRecordings;
            }

            if(!_validSampleRates.Contains(SampleRate))
            {
                SampleRate = _defaultSampleRate;
            }
            
            if(!_validChannelCounts.Contains(ChannelCount))
            {
                ChannelCount = _defaultChannelCount;
            }

            if(!_validMp3BitRates.Contains(Mp3BitRate))
            {
                Mp3BitRate = _defaultMp3BitRate;
            }

            if(string.IsNullOrEmpty(Genre))
            {
                Genre = Properties.Resources.SPEECH;
            }

            if(MaxRecordingTimeMins < 0)
            {
                MaxRecordingTimeMins = _defaultMaxRecordingMins;
            }

            if(RecordingDevice < 0)
            {
                RecordingDevice = _defaultRecordingDevice;
            }
        }

    }
}
