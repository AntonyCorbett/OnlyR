using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;
using System;
using System.Windows.Threading;

namespace OnlyR.Tests.Mocks;

/// <summary>
/// A mock audio service
/// </summary>
internal sealed class MockAudioService : IAudioService
{
    private readonly DispatcherTimer _timer;
    private readonly Random _random;
    private RecordingStatus _status;

    public MockAudioService()
    {
        this._status = RecordingStatus.NotRecording;
        this._random = new Random();

        this._timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        this._timer.Tick += RecordingTimer;
    }

    public event EventHandler? StartedEvent;

    public event EventHandler? StoppedEvent;

    public event EventHandler? StopRequested;

    public event EventHandler<RecordingProgressEventArgs>? RecordingProgressEvent;

    public event EventHandler? PausedEvent;

    public event EventHandler? ResumedEvent;

    public RecordingDeviceItem[] GetRecordingDeviceList()
    {
        return
        [
            new RecordingDeviceItem(1, "Dev1"),
            new RecordingDeviceItem(2, "Dev2")
        ];
    }

    public void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService)
    {
        if (this._status == RecordingStatus.NotRecording)
        {
            this._status = RecordingStatus.Recording;
            OnStartedEvent();
            this._timer.Start();
        }
    }

    public void StopRecording(bool fadeOut)
    {
        this._status = RecordingStatus.StopRequested;
        OnStopRequested();

        this._timer.Stop();
        OnStoppedEvent();
    }

    public void PauseRecording()
    {
        if (this._status == RecordingStatus.Recording)
        {
            this._status = RecordingStatus.Paused;
            this._timer.Stop();
            PausedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ResumeRecording()
    {
        if (this._status == RecordingStatus.Paused)
        {
            this._status = RecordingStatus.Recording;
            this._timer.Start();
            ResumedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnStartedEvent()
    {
        StartedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopRequested()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnStoppedEvent()
    {
        StoppedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecordingProgressEvent(RecordingProgressEventArgs e)
    {
        RecordingProgressEvent?.Invoke(this, e);
    }

    private void RecordingTimer(object? sender, EventArgs e)
    {
        OnRecordingProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = this._random.Next(0, 101) });
    }
}