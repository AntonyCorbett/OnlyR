using System;
using System.Runtime.Remoting.Messaging;
using NAudio.Wave;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;

namespace OnlyR.Core.Recorder
{
    public sealed class AudioRecorder : IDisposable
    {
        private WaveFileWriter _waveWriter;
        private WaveIn _waveSource;

        private readonly int _sampleRate = 44100;
        private readonly int _channelCount = 1;

        public AudioRecorder()
        {
            _recordingStatus = RecordingStatus.NotRecording;
        }

        private RecordingStatus _recordingStatus;
        public event EventHandler<RecordingStatusChangeEventArgs> RecordingStatusChangeEvent;

        public void Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _waveSource?.Dispose();
            _waveSource = null;

            _waveWriter?.Dispose();
            _waveWriter = null;
        }

        public void Start(string destFilePath)
        {
            if (_recordingStatus == RecordingStatus.NotRecording)
            {
                _waveSource = new WaveIn {WaveFormat = new WaveFormat(_sampleRate, _channelCount)};
                _waveSource.DataAvailable += WaveSourceOnDataAvailable;
                _waveSource.RecordingStopped += WaveSourceOnRecordingStopped;

                _waveWriter = new WaveFileWriter(destFilePath, _waveSource.WaveFormat);
                _waveSource.StartRecording();
                OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording));
            }
        }

        private void WaveSourceOnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Cleanup();
            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.NotRecording));
        }

        private void WaveSourceOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            _waveWriter.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
        }

        public void Stop()
        {
            if (_recordingStatus == RecordingStatus.Recording)
            {
                OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.StopRequested));
                _waveSource.StopRecording();
            }
        }

        private void OnRecordingStatusChangeEvent(RecordingStatusChangeEventArgs e)
        {
            _recordingStatus = e.RecordingStatus;
            RecordingStatusChangeEvent?.Invoke(this, e);
        }
    }
}
