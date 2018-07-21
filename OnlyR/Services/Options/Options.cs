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

        private static readonly int[] ValidSampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000 };
        private static readonly int DefaultSampleRate = 44100;

        private static readonly int[] ValidChannelCounts = { 1, 2 };
        private static readonly int DefaultChannelCount = 1;

        private static readonly int[] ValidMp3BitRates = { 320, 256, 224, 192, 160, 144, 128, 112, 96, 80, 64, 56, 48, 32 };
        private static readonly int DefaultMp3BitRate = 96;

        private static readonly int DefaultMaxRecordingMins = 0;  // no limit

        /// <summary>
        /// Maximum recordings in a single folder
        /// </summary>
        public int MaxRecordingsInOneFolder { get; set; }

        /// <summary>
        /// The audio sample rate used for recording
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Number of channels to record
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// Bit rate at which MP3 is encoded
        /// </summary>
        public int Mp3BitRate { get; set; }

        /// <summary>
        /// The music genre, placed in the MP3 Id3 tag
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// Maximum recording time (for a single recording). 0 = no limit
        /// </summary>
        public int MaxRecordingTimeMins { get; set; }

        /// <summary>
        /// Id of the chosen recording device. (0 is default)
        /// </summary>
        public int RecordingDevice { get; set; }

        /// <summary>
        /// Whether the recording should be automatically faded out
        /// </summary>
        public bool FadeOut { get; set; }

        /// <summary>
        /// Whether the recording should start automatically on launch
        /// </summary>
        public bool StartRecordingOnLaunch { get; set; }

        /// <summary>
        /// The final recording folder (leave empt for default).
        /// </summary>
        public string DestinationFolder { get; set; }

        public string AppWindowPlacement { get; set; }

        public bool AlwaysOnTop { get; set; }

        public bool AllowCloseWhenRecording { get; set; }

        public Options()
        {
            MaxRecordingsInOneFolder = DefaultMaxRecordings;
            SampleRate = DefaultSampleRate;
            ChannelCount = DefaultChannelCount;
            Mp3BitRate = DefaultMp3BitRate;
            Genre = Properties.Resources.SPEECH;
            MaxRecordingTimeMins = DefaultMaxRecordingMins;
            RecordingDevice = DefaultRecordingDevice;
            DestinationFolder = FileUtils.GetDefaultMyDocsDestinationFolder();
        }

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
                MaxRecordingTimeMins = DefaultMaxRecordingMins;
            }

            if (RecordingDevice < 0)
            {
                RecordingDevice = DefaultRecordingDevice;
            }
        }
    }
}
