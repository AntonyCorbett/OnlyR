namespace OnlyR.Core.EventArgs
{
    using Enums;

    /// <summary>
    /// Used to notify clients of a change in recording status
    /// </summary>
    public class RecordingStatusChangeEventArgs : System.EventArgs
    {
        public RecordingStatus RecordingStatus { get; }

        public RecordingStatusChangeEventArgs(RecordingStatus status)
        {
            RecordingStatus = status;
        }
    }
}
