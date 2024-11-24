using System;
using System.Collections.Generic;
using NAudio.Lame;
using NAudio.Wave;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Samples;

namespace OnlyR.Core.Recorder
{
    /// <summary>
    /// The audio recorder. Uses NAudio for the heavy lifting, but it's isolated in this class
    /// so if we need to replace NAudio with another library we just need to modify this part
    /// of the application.
    /// </summary>
    public sealed class AudioRecorder : IDisposable
    {
        // use these 2 together. Experiment to get the best VU display...
        private const int RequiredReportingIntervalMs = 40;
        private const int VuSpeed = 5;

        private LameMP3FileWriter? _mp3Writer;
        private WaveFileWriter? _waveFileWriter;
        private IWaveIn? _waveSource;
        private WaveOutEvent? _silenceWaveOut;
        private SampleAggregator? _sampleAggregator;
        private VolumeFader? _fader;
        private RecordingStatus _recordingStatus;
        private string? _tempRecordingFilePath;
        private string? _finalRecordingFilePath;

        private int _dampedLevel;

        public AudioRecorder()
        {
            _recordingStatus = RecordingStatus.NotRecording;
        }

        public event EventHandler<RecordingProgressEventArgs>? ProgressEvent;

        public event EventHandler<RecordingStatusChangeEventArgs>? RecordingStatusChangeEvent;

        /// <summary>
        /// Gets a list of Windows recording devices.
        /// </summary>
        /// <returns>Collection of devices.</returns>
        public static IEnumerable<RecordingDeviceInfo> GetRecordingDeviceList()
        {
            var result = new List<RecordingDeviceInfo>();

            var count = WaveIn.DeviceCount;
            for (var n = 0; n < count; ++n)
            {
                var caps = WaveIn.GetCapabilities(n);
                result.Add(new RecordingDeviceInfo(n, caps.ProductName));
            }

            return result;
        }

        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>
        /// Starts recording.
        /// </summary>
        /// <param name="recordingConfig">Recording configuration.</param>
        public void Start(RecordingConfig recordingConfig)
        {
            if (_recordingStatus == RecordingStatus.NotRecording)
            {
                CheckRecordingDevice(recordingConfig);

                if (recordingConfig.UseLoopbackCapture)
                {
                    _waveSource = new WasapiLoopbackCapture();
                    ConfigureSilenceOut();
                }
                else
                {
                    _waveSource = new WaveIn
                    {
                        WaveFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount),
                        DeviceNumber = recordingConfig.RecordingDevice,
                    };
                }

                InitAggregator(_waveSource.WaveFormat.SampleRate);
                InitFader(_waveSource.WaveFormat.SampleRate);

                _waveSource.DataAvailable += WaveSourceDataAvailableHandler;
                _waveSource.RecordingStopped += WaveSourceRecordingStoppedHandler;

                switch (recordingConfig.Codec)
                {
                    case AudioCodec.Mp3:
                        _mp3Writer = new LameMP3FileWriter(
                            recordingConfig.DestFilePath,
                            _waveSource.WaveFormat,
                            recordingConfig.Mp3BitRate,
                            CreateTag(recordingConfig));
                        break;
                    case AudioCodec.Wav:
                        _waveFileWriter = new WaveFileWriter(recordingConfig.DestFilePath, _waveSource.WaveFormat);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported codec");
                }

                _waveSource.StartRecording();

                _tempRecordingFilePath = recordingConfig.DestFilePath;
                _finalRecordingFilePath = recordingConfig.FinalFilePath;

                OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording)
                {
                    TempRecordingPath = _tempRecordingFilePath,
                    FinalRecordingPath = _finalRecordingFilePath
                });
            }
        }

        private void ConfigureSilenceOut()
        {
            // WasapiLoopbackCapture doesn't record any audio when nothing is playing
            // so we must play some silence!

            var silence = new SilenceProvider(new WaveFormat(44100, 2));
            _silenceWaveOut = new WaveOutEvent();
            _silenceWaveOut.Init(silence);
            _silenceWaveOut.Play();
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        /// <param name="fadeOut">true - fade out the recording instead of stopping immediately.</param>
        public void Stop(bool fadeOut)
        {
            if (_recordingStatus == RecordingStatus.Recording)
            {
                OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.StopRequested)
                {
                    TempRecordingPath = _tempRecordingFilePath,
                    FinalRecordingPath = _finalRecordingFilePath,
                });

                if (fadeOut)
                {
                    _fader?.Start();
                }
                else
                {
                    _waveSource?.StopRecording();
                    _silenceWaveOut?.Stop();
                }
            }
        }

        private static ID3TagData CreateTag(RecordingConfig recordingConfig)
        {
            // tag is embedded as MP3 metadata
            return new()
            {
                Title = recordingConfig.TrackTitle,
                Album = recordingConfig.AlbumName,
                Track = recordingConfig.TrackNumber.ToString(),
                Genre = recordingConfig.Genre,
                Year = recordingConfig.RecordingDate.Year.ToString(),
            };
        }

        private static void CheckRecordingDevice(RecordingConfig recordingConfig)
        {
            var deviceCount = WaveIn.DeviceCount;
            if (deviceCount == 0)
            {
                throw new NoDevicesException();
            }

            if (!recordingConfig.UseLoopbackCapture && recordingConfig.RecordingDevice >= deviceCount)
            {
                recordingConfig.RecordingDevice = 0;
            }
        }

        private void InitAggregator(int sampleRate)
        {
            // the aggregator collects audio sample metrics 
            // and publishes the results at suitable intervals.
            // Used by the OnlyR volume meter
            if (_sampleAggregator != null)
            {
                _sampleAggregator.ReportEvent -= AggregatorReportHandler;
            }

            _sampleAggregator = new SampleAggregator(sampleRate, RequiredReportingIntervalMs);
            _sampleAggregator.ReportEvent += AggregatorReportHandler;
        }

        private void AggregatorReportHandler(object? sender, SamplesReportEventArgs e)
        {
            var value = Math.Max(e.MaxSample, Math.Abs(e.MinSample)) * 100;

            var damped = GetDampedVolumeLevel(value);
            OnProgressEvent(new RecordingProgressEventArgs { VolumeLevelAsPercentage = damped });
        }

        private void WaveSourceRecordingStoppedHandler(object? sender, StoppedEventArgs e)
        {
            Cleanup();
            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.NotRecording));
            _fader = null;
        }

        private void WaveSourceDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
        {
            // as audio samples are provided by WaveIn, we hook in here 
            // and write them to disk, encoding to MP3 on the fly 
            // using the _mp3Writer.
            var buffer = waveInEventArgs.Buffer;
            var bytesRecorded = waveInEventArgs.BytesRecorded;

            var isFloatingPointAudio = _waveSource?.WaveFormat.BitsPerSample == 32;

            if (_fader != null && _fader.Active)
            {
                // we're fading out...
                _fader.FadeBuffer(buffer, bytesRecorded, isFloatingPointAudio);
            }

            AddToSampleAggregator(buffer, bytesRecorded, isFloatingPointAudio);

            _waveFileWriter?.Write(buffer, 0, bytesRecorded);
            _mp3Writer?.Write(buffer, 0, bytesRecorded);
        }

        private void AddToSampleAggregator(byte[] buffer, int bytesRecorded, bool isFloatingPointAudio)
        {
            var buff = new WaveBuffer(buffer);

            if (isFloatingPointAudio)
            {
                for (var index = 0; index < bytesRecorded / 4; ++index)
                {
                    var sample = buff.FloatBuffer[index];
                    _sampleAggregator?.Add(sample);
                }
            }
            else
            {
                for (var index = 0; index < bytesRecorded / 2; ++index)
                {
                    var sample = buff.ShortBuffer[index];
                    _sampleAggregator?.Add(sample / 32768F);
                }
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
            // provide some "damping" of the volume meter.
            if (volLevel > _dampedLevel)
            {
                _dampedLevel = (int)(volLevel + VuSpeed);
            }

            _dampedLevel -= VuSpeed;
            if (_dampedLevel < 0)
            {
                _dampedLevel = 0;
            }

            return _dampedLevel;
        }

        private void FadeCompleteHandler(object? sender, System.EventArgs e)
        {
            _waveSource?.StopRecording();
            _silenceWaveOut?.Stop();
        }

        private void Cleanup()
        {
            _mp3Writer?.Flush();
            _waveFileWriter?.Flush();

            _waveSource?.Dispose();
            _waveSource = null;

            _silenceWaveOut?.Dispose();
            _silenceWaveOut = null;

            _mp3Writer?.Dispose();
            _mp3Writer = null;

            _waveFileWriter?.Dispose();
            _waveFileWriter = null;

            _tempRecordingFilePath = null;
        }

        private void InitFader(int sampleRate)
        {
            // used to optionally fade out a recording
            _fader = new VolumeFader(sampleRate);
            _fader.FadeComplete += FadeCompleteHandler;
        }
    }
}
