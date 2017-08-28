using System;

namespace OnlyR.Core.Recorder
{
    public class RecordingConfig
    {
        public int RecordingDevice { get; set; }
        public DateTime RecordingDate { get; set; }
        public int TrackNumber { get; set; }
        public string DestFilePath { get; set; }
        public int SampleRate { get; set; }
        public int ChannelCount { get; set; }
        public int Mp3BitRate { get; set; }
        public string TrackTitle { get; set; }
        public string AlbumName { get; set; }
        public string Genre { get; set; }
    }
}
