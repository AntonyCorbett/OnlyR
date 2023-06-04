namespace OnlyR.Model
{
    /// <summary>
    /// State object optionally passed to Recording page on navigation
    /// </summary>
    internal sealed class RecordingPageNavigationState
    {
        public bool ShowSplash { get; set; }

        public bool StartRecording { get; set; }
    }
}
