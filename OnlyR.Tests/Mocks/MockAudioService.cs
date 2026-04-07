using System;
using System.Windows.Threading;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Model;
using OnlyR.Services.Audio;
using OnlyR.Services.Options;

namespace OnlyR.Tests.Mocks;

/// <summary>
/// A mock audio service
/// </summary>
internal sealed class MockAudioService : IAudioService
{
    private readonly DispatcherTimer timer;
    private readonly Random random;
    private RecordingStatus status;

    public MockAudioService()
    {
        this.status = RecordingStatus.NotRecording;
        this.random = new Random();

        this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        this.timer.Tick += RecordingTimer;
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
        if (this.status == RecordingStatus.NotRecording)
        {
            this.status = RecordingStatus.Recording;
            OnStartedEvent();
            this.timer.Start();
        }
    }

    public void StopRecording(bool fadeOut)
    {
        this.status = RecordingStatus.StopRequested;
        OnStopRequested();

        this.timer.Stop();
        OnStoppedEvent();
    }

    public void PauseRecording()
    {
        if (this.status == RecordingStatus.Recording)
        {
            this.status = RecordingStatus.Paused;
            this.timer.Stop();
            PausedEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ResumeRecording()
    {
        if (this.status == RecordingStatus.Paused)
        {
            this.status = RecordingStatus.Recording;
            this.timer.Start();
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
        OnRecordingProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = this.random.Next(0, 101) });
    }
}