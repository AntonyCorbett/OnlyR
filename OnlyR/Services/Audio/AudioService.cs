using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Recorder;
using OnlyR.Model;

namespace OnlyR.Services.Audio
{
    public sealed class AudioService : IAudioService, IDisposable
    {
        private AudioRecorder _audioRecorder;

        public event EventHandler StartedEvent;
        public event EventHandler StoppedEvent;
        public event EventHandler StopRequested;

        public AudioService()
        {
            _audioRecorder = new AudioRecorder();
            _audioRecorder.RecordingStatusChangeEvent += AudioRecorderOnRecordingStatusChangeEvent;
        }

        private void AudioRecorderOnRecordingStatusChangeEvent(object sender, RecordingStatusChangeEventArgs recordingStatusChangeEventArgs)
        {
            switch(recordingStatusChangeEventArgs.RecordingStatus)
            {
                case RecordingStatus.NotRecording:
                    OnStoppedEvent();
                    break;
                case RecordingStatus.Recording:
                    OnStartedEvent();
                    break;
                case RecordingStatus.StopRequested:
                    OnStopRequested();
                    break;
            }
        }

        public void StartRecording(RecordingCandidate candidateFile)
        {
            _audioRecorder.Start(candidateFile.TempPath);
        }

        public void StopRecording()
        {
            _audioRecorder.Stop();
        }

        private void OnStartedEvent()
        {
            StartedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnStoppedEvent()
        {
            StoppedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnStopRequested()
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _audioRecorder.Dispose();
            _audioRecorder = null;
        }
    }
}
