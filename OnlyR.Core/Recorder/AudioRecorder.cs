using System;
using NAudio.Lame;
using NAudio.Wave;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Samples;

namespace OnlyR.Core.Recorder
{
    public sealed class AudioRecorder : IDisposable
    {
        private LameMP3FileWriter _mp3Writer;
        private WaveIn _waveSource;
        private SampleAggregator _sampleAggregator;

        private int _dampedLevel;

        // use these 2 together. Experiment to get the best VU display...
        private readonly int _requiredReportingIntervalMs = 40;
        private readonly int _vuSpeed = 5;  
        

        public event EventHandler<RecordingProgressEventArgs> ProgressEvent;

        public AudioRecorder()
        {
            _recordingStatus = RecordingStatus.NotRecording;
        }

        private RecordingStatus _recordingStatus;
        public event EventHandler<RecordingStatusChangeEventArgs> RecordingStatusChangeEvent;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_mp3Writer")]
        public void Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _waveSource?.Dispose();
            _waveSource = null;

            _mp3Writer?.Dispose();
            _mp3Writer = null;
        }

        private static ID3TagData CreateTag(RecordingConfig recordingConfig)
        {
            return new ID3TagData
            {
                Title = recordingConfig.TrackTitle,
                Album = recordingConfig.AlbumName,
                Track = recordingConfig.TrackNumber.ToString(),
                Genre = recordingConfig.Genre,
                Year = recordingConfig.RecordingDate.Year.ToString(),
                UserDefinedTags = new string []{}     // fix bug in naudio.lame
            };
        }

        public void Start(RecordingConfig recordingConfig)
        {
            if (_recordingStatus == RecordingStatus.NotRecording)
            {
                InitAggregator(recordingConfig.SampleRate);

                _waveSource = new WaveIn
                {
                    WaveFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount)
                };

                _waveSource.DataAvailable += WaveSourceDataAvailableHandler;
                _waveSource.RecordingStopped += WaveSourceRecordingStoppedHandler;
                
                _mp3Writer = new LameMP3FileWriter(recordingConfig.DestFilePath, _waveSource.WaveFormat,
                    recordingConfig.Mp3BitRate, CreateTag(recordingConfig));

                _waveSource.StartRecording();
                OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording));
            }
        }

        private void InitAggregator(int sampleRate)
        {
            if (_sampleAggregator != null)
            {
                _sampleAggregator.ReportEvent -= AggregatorReportHandler;
            }

            _sampleAggregator = new SampleAggregator(sampleRate, _requiredReportingIntervalMs);
            _sampleAggregator.ReportEvent += AggregatorReportHandler;
        }

        private void AggregatorReportHandler(object sender, SamplesReportEventArgs e)
        {
            float value = Math.Max(e.MaxSample, Math.Abs(e.MinSample)) * 100;
            OnProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = GetDampedVolumeLevel(value) });
        }

        private void WaveSourceRecordingStoppedHandler(object sender, StoppedEventArgs e)
        {
            Cleanup();
            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.NotRecording));
        }

        private void WaveSourceDataAvailableHandler(object sender, WaveInEventArgs waveInEventArgs)
        {
            byte[] buffer = waveInEventArgs.Buffer;
            int bytesRecorded = waveInEventArgs.BytesRecorded;

            for (int index = 0; index < bytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) | buffer[index + 0]);
                float sample32 = sample / 32768f;
                _sampleAggregator.Add(sample32);
            }

            _mp3Writer.Write(buffer, 0, bytesRecorded);
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

        private void OnProgressEvent(RecordingProgressEventArgs e)
        {
            ProgressEvent?.Invoke(this, e);
        }

        private int GetDampedVolumeLevel(float volLevel)
        {
            if (volLevel > _dampedLevel)
            {
                _dampedLevel = (int)(volLevel + _vuSpeed);
            }

            _dampedLevel -= _vuSpeed;
            if (_dampedLevel < 0)
            {
                _dampedLevel = 0;
            }

            return _dampedLevel;
        }

    }
}
