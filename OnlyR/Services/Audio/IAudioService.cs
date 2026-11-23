using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Options;
using System;

namespace OnlyR.Services.Audio;

public interface IAudioService
{
    event EventHandler StartedEvent;

    event EventHandler StoppedEvent;

    /// <summary>
    /// Raised after the recording has stopped and the file has been moved to its
    /// final destination.
    /// </summary>
    event EventHandler RecordingFinalisedEvent;

    /// <summary>
    /// Gets a value indicating whether a recording is being started, is in progress,
    /// or is being finalised (the file has not yet reached its final destination).
    /// </summary>
    bool HasPendingRecording { get; }

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