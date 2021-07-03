using OnlyR.Core.Enums;

namespace OnlyR.Core.EventArgs
{
    /// <summary>
    /// Used to notify clients of a change in recording status
    /// </summary>
    public class RecordingStatusChangeEventArgs : System.EventArgs
    {
        public RecordingStatusChangeEventArgs(RecordingStatus status)
        {
            RecordingStatus = status;
        }

        public RecordingStatus RecordingStatus { get; }
    }
}
