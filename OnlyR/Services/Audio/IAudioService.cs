namespace OnlyR.Services.Audio
{
    using System;
    using Core.EventArgs;
    using Model;
    using Options;

    public interface IAudioService
    {
        event EventHandler StartedEvent;

        event EventHandler StoppedEvent;

        event EventHandler StopRequested;

        event EventHandler<RecordingProgressEventArgs> RecordingProgressEvent;

        RecordingDeviceItem[] GetRecordingDeviceList();

        void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService);

        void StopRecording(bool fadeOut);
    }
}
