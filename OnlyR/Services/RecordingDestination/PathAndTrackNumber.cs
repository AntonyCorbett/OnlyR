namespace OnlyR.Services.RecordingDestination
{
    /// <summary>
    /// Represents a file path and recording track number
    /// </summary>
    internal class PathAndTrackNumber
    {
        public PathAndTrackNumber(string filePath, int trackNumber)
        {
            FilePath = filePath;
            TrackNumber = trackNumber;
        }

        public string FilePath { get; }

        public int TrackNumber { get; }
    }
}
