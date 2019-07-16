namespace OnlyR.Services.Options
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Utils;

    /// <summary>
    /// All program options. The full structure is written to disk in JSON format on change
    /// of data, and read from disk during app startup
    /// </summary>
    public class Options
    {
        private const int DefaultMaxRecordings = 999;
        private const int DefaultRecordingDevice = 0;
        private const int DefaultMaxRecordingMinutes = 0; // no limit
        private const int DefaultSampleRate = 44100;
        private const int DefaultChannelCount = 1;
        private const int DefaultMp3BitRate = 96;

        private static readonly int[] ValidSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000 };
        private static readonly int[] ValidChannelCounts = { 1, 2 };
        private static readonly int[] ValidMp3BitRates = { 320, 256, 224, 192, 160, 144, 128, 112, 96, 80, 64, 56, 48, 32 };

        public Options()
        {
            MaxRecordingsInOneFolder = DefaultMaxRecordings;
            SampleRate = DefaultSampleRate;
            ChannelCount = DefaultChannelCount;
            Mp3BitRate = DefaultMp3BitRate;
            Genre = Properties.Resources.SPEECH;
            MaxRecordingTimeMins = DefaultMaxRecordingMinutes;
            RecordingDevice = DefaultRecordingDevice;
            DestinationFolder = FileUtils.GetDefaultMyDocsDestinationFolder();
            RecordingsLifeTimeDays = 0; // forever
        }

        public int MaxRecordingsInOneFolder { get; set; }

        public int SampleRate { get; set; }

        public int ChannelCount { get; set; }

        public int Mp3BitRate { get; set; }

        public string Genre { get; set; }

        public int MaxRecordingTimeMins { get; set; }

        public int RecordingDevice { get; set; }

        public bool FadeOut { get; set; }

        public bool StartRecordingOnLaunch { get; set; }

        public string DestinationFolder { get; set; }

        public string AppWindowPlacement { get; set; }

        public bool AlwaysOnTop { get; set; }

        public bool AllowCloseWhenRecording { get; set; }

        public int RecordingsLifeTimeDays { get; set; }

        public string Culture { get; set; }

        public static IEnumerable<int> GetSupportedSampleRates()
        {
            return ValidSampleRates;
        }

        public static IEnumerable<int> GetSupportedChannels()
        {
            return ValidChannelCounts;
        }

        public static IEnumerable<int> GetSupportedMp3BitRates()
        {
            return ValidMp3BitRates;
        }

        /// <summary>
        /// Validates the data, correcting automatically as required
        /// </summary>
        public void Sanitize()
        {
            Debug.Assert(ValidChannelCounts.Contains(DefaultChannelCount), "ValidChannelCounts.Contains(DefaultChannelCount)");
            Debug.Assert(ValidSampleRates.Contains(DefaultSampleRate), "ValidSampleRates.Contains(DefaultSampleRate)");
            Debug.Assert(ValidMp3BitRates.Contains(DefaultMp3BitRate), "ValidMp3BitRates.Contains(DefaultMp3BitRate)");

            if (RecordingsLifeTimeDays < 0)
            {
                RecordingsLifeTimeDays = 0;
            }

            if (MaxRecordingsInOneFolder < 10 || MaxRecordingsInOneFolder > 500)
            {
                MaxRecordingsInOneFolder = DefaultMaxRecordings;
            }

            if (!ValidSampleRates.Contains(SampleRate))
            {
                SampleRate = DefaultSampleRate;
            }

            if (!ValidChannelCounts.Contains(ChannelCount))
            {
                ChannelCount = DefaultChannelCount;
            }

            if (!ValidMp3BitRates.Contains(Mp3BitRate))
            {
                Mp3BitRate = DefaultMp3BitRate;
            }

            if (string.IsNullOrEmpty(Genre))
            {
                Genre = Properties.Resources.SPEECH;
            }

            if (MaxRecordingTimeMins < 0)
            {
                MaxRecordingTimeMins = DefaultMaxRecordingMinutes;
            }

            if (RecordingDevice < 0)
            {
                RecordingDevice = DefaultRecordingDevice;
            }
        }
    }
}
