using System;
using OnlyR.Model;

namespace OnlyR.Services.Audio
{
    public interface IAudioService
    {
        event EventHandler StartedEvent;
        event EventHandler StoppedEvent;
        event EventHandler StopRequested;

        void StartRecording(RecordingCandidate candidateFile);
        void StopRecording();
    }
}
