using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Samples;

namespace OnlyR.Core.Recorder;

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

    private MixingSampleProvider? _mixingSampleProvider;

    private Stream? _audioWriter;
    private WasapiLoopbackCapture? _captureSource;
    private WaveIn? _deviceSource;
    private WaveOutEvent? _silenceWaveOut;
    private SampleAggregator? _sampleAggregator;
    private VolumeFader? _fader;
    private RecordingStatus _recordingStatus = RecordingStatus.NotRecording;
    private string? _tempRecordingFilePath;
    private string? _finalRecordingFilePath;

    private int _dampedLevel;

    public event EventHandler<RecordingProgressEventArgs>? ProgressEvent;

    public event EventHandler<RecordingStatusChangeEventArgs>? RecordingStatusChangeEvent;

    /// <summary>
    /// Gets a list of Windows recording devices.
    /// </summary>
    /// <returns>Collection of devices.</returns>
    public static IList<RecordingDeviceInfo> GetRecordingDeviceList()
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
            
            var mixingSampleProviders = new List<ISampleProvider>();

            if (recordingConfig.UseLoopbackCapture)
            {
                _captureSource = new WasapiLoopbackCapture();
                ConfigureSilenceOut();

                // Convert WasapiLoopbackCapture to ISampleProvider
                var waveProvider = new WaveInProvider(_captureSource);
                var sampleProvider = new WaveToSampleProvider(waveProvider);
                mixingSampleProviders.Add(sampleProvider);
            }

            if (recordingConfig.RecordingDevice != RecordingConfig.EmptyRecordingDeviceId)
            {
                _deviceSource = new WaveIn
                {
                    WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount),
                    DeviceNumber = recordingConfig.RecordingDevice,
                };

                var waveProvider = new WaveInProvider(_deviceSource);
                var sampleProvider = new WaveToSampleProvider(waveProvider);
                mixingSampleProviders.Add(sampleProvider);
            }

            _mixingSampleProvider = new MixingSampleProvider(mixingSampleProviders);
            _mixingSampleProvider.ReadFully = true; // ensure we read all samples

            InitAggregator(_mixingSampleProvider.WaveFormat.SampleRate);
            InitFader(_mixingSampleProvider.WaveFormat.SampleRate);

            SubscribeHandlers();

            var format = _deviceSource?.WaveFormat ?? _captureSource!.WaveFormat;

            _audioWriter = recordingConfig.Codec switch
                {
                    AudioCodec.Mp3 => new LameMP3FileWriter(
                        recordingConfig.DestFilePath,
                        format,
                        recordingConfig.Mp3BitRate!.Value,
                        CreateTag(recordingConfig)),
                    AudioCodec.Wav => new WaveFileWriter(recordingConfig.DestFilePath, format),
                    _ => throw new NotSupportedException("Unsupported codec"),
                };

            _deviceSource?.StartRecording();
            _captureSource?.StartRecording();

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
                _deviceSource?.StopRecording();
                _captureSource?.StopRecording();

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
            Track = recordingConfig.TrackNumber.ToString(CultureInfo.InvariantCulture),
            Genre = recordingConfig.Genre,
            Year = recordingConfig.RecordingDate.Year.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static void CheckRecordingDevice(RecordingConfig recordingConfig)
    {
        if (recordingConfig.RecordingDevice >= WaveIn.DeviceCount)
        {
            recordingConfig.RecordingDevice = RecordingConfig.EmptyRecordingDeviceId;
        }

        if (recordingConfig.RecordingDevice == RecordingConfig.EmptyRecordingDeviceId && 
            !recordingConfig.UseLoopbackCapture)
        {
            throw new NoDevicesException();
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
        if (_mixingSampleProvider == null) return;

        // Create a buffer for the floating-point samples
        var sampleCount = waveInEventArgs.BytesRecorded / 4; // 4 bytes per float
        var floatBuffer = new float[sampleCount];

        // Read from the mixing provider
        var samplesRead = _mixingSampleProvider.Read(floatBuffer, 0, sampleCount);

        if (samplesRead > 0)
        {
            // Apply fading if active
            if (_fader?.Active == true)
            {
                _fader.FadeBuffer(floatBuffer, samplesRead);
            }

            // Add to sample aggregator for VU meter
            for (var i = 0; i < samplesRead; i++)
            {
                _sampleAggregator?.Add(floatBuffer[i]);
            }

            // Convert float samples to bytes for the audio writer
            var bytesBuffer = new byte[samplesRead * 4];
            Buffer.BlockCopy(floatBuffer, 0, bytesBuffer, 0, samplesRead * 4);

            // Write to the output file
            _audioWriter?.Write(bytesBuffer, 0, samplesRead * 4);
        }
    }

    //private void WaveSourceDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
    //{
    //    // as audio samples are provided by WaveIn, we hook in here 
    //    // and write them to disk, (encoding to MP3 on the fly if needed)
    //    var buffer = waveInEventArgs.Buffer;
    //    var bytesRecorded = waveInEventArgs.BytesRecorded;

    //    var isFloatingPointAudio = _waveSource?.WaveFormat.BitsPerSample == 32;

    //    if (_fader?.Active == true)
    //    {
    //        // we're fading out...
    //        _fader.FadeBuffer(buffer, bytesRecorded, isFloatingPointAudio);
    //    }

    //    AddToSampleAggregator(buffer, bytesRecorded, isFloatingPointAudio);

    //    _audioWriter?.Write(buffer, 0, bytesRecorded);
    //}

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
        _deviceSource?.StopRecording();
        _captureSource?.StopRecording();
        _silenceWaveOut?.Stop();
    }

    private void SubscribeHandlers()
    {
        // either _deviceSource or _captureSource events will be subscribed (not both)
        if (_deviceSource != null)
        {
            _deviceSource.DataAvailable += WaveSourceDataAvailableHandler;
            _deviceSource.RecordingStopped += WaveSourceRecordingStoppedHandler;
        }
        else if(_captureSource != null)
        {
            _captureSource.DataAvailable += WaveSourceDataAvailableHandler;
            _captureSource.RecordingStopped += WaveSourceRecordingStoppedHandler;
        }
    }

    private void UnsubscribeHandlers()
    {
        // either _deviceSource or _captureSource events will be subscribed (not both)
        if (_deviceSource != null)
        {
            _deviceSource.DataAvailable -= WaveSourceDataAvailableHandler;
            _deviceSource.RecordingStopped -= WaveSourceRecordingStoppedHandler;
        }
        else if (_captureSource != null)
        {
            _captureSource.DataAvailable -= WaveSourceDataAvailableHandler;
            _captureSource.RecordingStopped -= WaveSourceRecordingStoppedHandler;
        }
    }

    private void Cleanup()
    {
        _audioWriter?.Flush();

        UnsubscribeHandlers();

        _mixingSampleProvider?.RemoveAllMixerInputs();
            
        _deviceSource?.Dispose();
        _deviceSource = null;
        
        _captureSource?.Dispose();
        _captureSource = null;
        
        _silenceWaveOut?.Dispose();
        _silenceWaveOut = null;

        _audioWriter?.Dispose();
        _audioWriter = null;

        _tempRecordingFilePath = null;
    }

    private void InitFader(int sampleRate)
    {
        // used to optionally fade out a recording
        _fader = new VolumeFader(sampleRate);
        _fader.FadeComplete += FadeCompleteHandler;
    }
}