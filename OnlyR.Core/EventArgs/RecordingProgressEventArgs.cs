namespace OnlyR.Core.EventArgs
{
    /// <summary>
    /// Used to notify clients of current recording level, etc
    /// </summary>
    public class RecordingProgressEventArgs : System.EventArgs
    {
        public int VolumeLevelAsPercentage { get; set; }
    }
}
