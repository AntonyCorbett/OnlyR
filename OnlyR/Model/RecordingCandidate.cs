using System;

namespace OnlyR.Model
{
    /// <summary>
    /// Represents a planned recording, with properties based on existing content of the recordings folder.
    /// </summary>
    public class RecordingCandidate
    {
        public RecordingCandidate(DateTime recordingDate, int trackNumber, string tempPath, string finalPath)
        {
            RecordingDate = recordingDate;
            TrackNumber = trackNumber;
            TempPath = tempPath;
            FinalPath = finalPath;
        }

        public DateTime RecordingDate { get; }

        public int TrackNumber { get; }

        public string TempPath { get; }

        public string FinalPath { get; }
    }
}
