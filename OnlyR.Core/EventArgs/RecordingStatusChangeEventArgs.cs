using OnlyR.Core.Enums;

namespace OnlyR.Core.EventArgs
{
    public class RecordingStatusChangeEventArgs : System.EventArgs
    {
        public RecordingStatus RecordingStatus { get; }

        public RecordingStatusChangeEventArgs(RecordingStatus status)
        {
            RecordingStatus = status;
        }
    }
}
