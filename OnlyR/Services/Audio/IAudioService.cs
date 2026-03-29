using System;
using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Options;

namespace OnlyR.Services.Audio
{
    public interface IAudioService
    {
        event EventHandler StartedEvent;

        event EventHandler StoppedEvent;

        event EventHandler StopRequested;

        event EventHandler<RecordingProgressEventArgs> RecordingProgressEvent;

        event EventHandler PausedEvent;

        event EventHandler ResumedEvent;

        RecordingDeviceItem[] GetRecordingDeviceList();

        void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService);

        void StopRecording(bool fadeOut);

        void PauseRecording();

        void ResumeRecording();
    }
}
