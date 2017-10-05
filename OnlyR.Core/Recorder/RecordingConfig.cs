using System;

namespace OnlyR.Core.Recorder
{
    /// <summary>
    /// Configuration of a reording
    /// </summary>
    public class RecordingConfig
    {
        /// <summary>
        /// The audio recording device Id
        /// </summary>
        public int RecordingDevice { get; set; }

        /// <summary>
        /// The date of the recording (the start date)
        /// </summary>
        public DateTime RecordingDate { get; set; }

        /// <summary>
        /// The track number
        /// </summary>
        public int TrackNumber { get; set; }

        /// <summary>
        /// The destination file path
        /// </summary>
        public string DestFilePath { get; set; }

        /// <summary>
        /// The audio sample rate
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        ///  The channel count
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// The bit rate at which we want to encode to MP3
        /// </summary>
        public int Mp3BitRate { get; set; }

        /// <summary>
        /// Teh title of the track (used in file name and written to MP3 tag title)
        /// </summary>
        public string TrackTitle { get; set; }

        /// <summary>
        /// Album name (written to MP3 tag)
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// Genre (written to MP3 tag)
        /// </summary>
        public string Genre { get; set; }
    }
}
