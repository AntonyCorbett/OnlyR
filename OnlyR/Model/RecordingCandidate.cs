using System;

namespace OnlyR.Model
{
    public class RecordingCandidate
    {
        public DateTime RecordingDate { get; set; }
        public int TrackNumber { get; set; }
        public string TempPath { get; set; }
        public string FinalPath { get; set; }
    }
}
