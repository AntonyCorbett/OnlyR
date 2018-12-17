namespace OnlyR.Tests.Mocks
{
    using System;
    using System.Windows.Threading;
    using Core.Enums;
    using Core.EventArgs;
    using Model;
    using Services.Audio;
    using Services.Options;

    /// <summary>
    /// A mock audio service
    /// </summary>
    internal class MockAudioService : IAudioService
    {
        private readonly DispatcherTimer _timer;
        private readonly Random _random;
        private RecordingStatus _status;

        public MockAudioService()
        {
            _status = RecordingStatus.NotRecording;
            _random = new Random();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _timer.Tick += RecordingTimer;
        }

        public event EventHandler StartedEvent;

        public event EventHandler StoppedEvent;

        public event EventHandler StopRequested;

        public event EventHandler<RecordingProgressEventArgs> RecordingProgressEvent;

        public RecordingDeviceItem[] GetRecordingDeviceList()
        {
            return new[]
            {
                new RecordingDeviceItem { DeviceName = "Dev1", DeviceId = 1 },
                new RecordingDeviceItem { DeviceName = "Dev1", DeviceId = 2 }
            };
        }

        public void StartRecording(RecordingCandidate candidateFile, IOptionsService optionsService)
        {
            if (_status == RecordingStatus.NotRecording)
            {
                _status = RecordingStatus.Recording;
                OnStartedEvent();
                _timer.Start();
            }
        }

        public void StopRecording(bool fadeOut)
        {
            _status = RecordingStatus.StopRequested;
            OnStopRequested();

            _timer.Stop();
            OnStoppedEvent();
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

        private void RecordingTimer(object sender, EventArgs e)
        {
            OnRecordingProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = _random.Next(0, 101) });
        }
    }
}
