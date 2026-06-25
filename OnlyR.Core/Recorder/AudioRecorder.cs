using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Samples;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace OnlyR.Core.Recorder;

/// <summary>
/// The audio recorder. Uses NAudio for the heavy lifting, but it's isolated in this class
/// so if we need to replace NAudio with another library we just need to modify this part
/// of the application.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AudioRecorder : IDisposable
{
    // use these 2 together. Experiment to get the best VU display...
    private const int RequiredReportingIntervalMs = 40;
    private const int VuSpeed = 5;

    // Caps how far the microphone buffer may grow in the mixed path, bounding
    // drift between the two independent capture clocks.
    private const int MixBufferSeconds = 5;

    private Stream? _audioWriter;
    private IWaveIn? _waveSource;
    private WaveOutEvent? _silenceWaveOut;
    private SampleAggregator? _sampleAggregator;
    private VolumeFader? _fader;
    private RecordingStatus _recordingStatus;
    private bool _isPaused;
    private string? _tempRecordingFilePath;
    private string? _finalRecordingFilePath;

    // Mixed (microphone + loopback) recording path. Only used when both sources are active.
    private WaveInEvent? _micCapture;
    private WasapiLoopbackCapture? _loopbackCapture;
    private BufferedWaveProvider? _micBuffer;
    private BufferedWaveProvider? _loopbackBuffer;
    private MixingSampleProvider? _mixer;
    private float[]? _mixSampleBuffer;
    private byte[]? _mixPcmBuffer;
    private int _outputSampleRate;
    private int _outputChannelCount;

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

            var captureMic = recordingConfig.RecordingDevice != RecordingConfig.EmptyRecordingDeviceId;

            if (captureMic && recordingConfig.UseLoopbackCapture)
            {
                StartMixedRecording(recordingConfig);
            }
            else
            {
                StartSingleSourceRecording(recordingConfig);
            }

            _tempRecordingFilePath = recordingConfig.DestFilePath;
            _finalRecordingFilePath = recordingConfig.FinalFilePath;

            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording)
            {
                TempRecordingPath = _tempRecordingFilePath,
                FinalRecordingPath = _finalRecordingFilePath
            });
        }
    }

    // Single-source recording (microphone-only or loopback-only) - the original, unchanged pipeline.
    private void StartSingleSourceRecording(RecordingConfig recordingConfig)
    {
        if (recordingConfig.UseLoopbackCapture)
        {
            _waveSource = new WasapiLoopbackCapture();
            ConfigureSilenceOut(_waveSource.WaveFormat);
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

        _audioWriter = CreateAudioWriter(recordingConfig, _waveSource.WaveFormat);

        _waveSource.StartRecording();
    }

    // Mixed recording: microphone + system loopback combined into a single output stream.
    // Each capture fills its own buffer; reads are clocked off the loopback capture (kept ticking
    // by the silence-out trick) and the two streams are summed via a MixingSampleProvider.
    private void StartMixedRecording(RecordingConfig recordingConfig)
    {
        _outputSampleRate = recordingConfig.SampleRate;
        _outputChannelCount = recordingConfig.ChannelCount;

        // Output matches single-source recordings: 16-bit PCM at the configured rate/channels.
        var outputFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount);

        _micCapture = new WaveInEvent
        {
            WaveFormat = outputFormat,
            DeviceNumber = recordingConfig.RecordingDevice,
        };
        _micBuffer = new BufferedWaveProvider(_micCapture.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(MixBufferSeconds),
        };
        _micCapture.DataAvailable += MicCaptureDataAvailableHandler;

        _loopbackCapture = new WasapiLoopbackCapture();
        _loopbackBuffer = new BufferedWaveProvider(_loopbackCapture.WaveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromSeconds(MixBufferSeconds),
        };
        _loopbackCapture.DataAvailable += LoopbackCaptureDataAvailableHandler;
        _loopbackCapture.RecordingStopped += WaveSourceRecordingStoppedHandler;

        ConfigureSilenceOut(_loopbackCapture.WaveFormat);

        var mixFormat = WaveFormat.CreateIeeeFloatWaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount);
        _mixer = new MixingSampleProvider(mixFormat) { ReadFully = true };
        _mixer.AddMixerInput(ConvertToOutputFormat(_micBuffer.ToSampleProvider()));
        _mixer.AddMixerInput(ConvertToOutputFormat(_loopbackBuffer.ToSampleProvider()));

        InitAggregator(recordingConfig.SampleRate);
        InitFader(recordingConfig.SampleRate);

        _audioWriter = CreateAudioWriter(recordingConfig, outputFormat);

        _micCapture.StartRecording();
        _loopbackCapture.StartRecording();
    }

    private static Stream CreateAudioWriter(RecordingConfig recordingConfig, WaveFormat waveFormat)
    {
        return recordingConfig.Codec switch
        {
            AudioCodec.Mp3 => new LameMP3FileWriter(
                recordingConfig.DestFilePath,
                waveFormat,
                recordingConfig.Mp3BitRate!.Value,
                CreateTag(recordingConfig)),
            AudioCodec.Wav => new WaveFileWriter(recordingConfig.DestFilePath, waveFormat),
            _ => throw new NotSupportedException("Unsupported codec"),
        };
    }

    // Converts a capture to the output format (channel count then sample rate) so it can be mixed.
    private ISampleProvider ConvertToOutputFormat(ISampleProvider source)
    {
        var result = MatchChannels(source, _outputChannelCount);

        if (result.WaveFormat.SampleRate != _outputSampleRate)
        {
            result = new WdlResamplingSampleProvider(result, _outputSampleRate);
        }

        return result;
    }

    private static ISampleProvider MatchChannels(ISampleProvider source, int targetChannels)
    {
        var channels = source.WaveFormat.Channels;
        if (channels == targetChannels)
        {
            return source;
        }

        if (channels == 2 && targetChannels == 1)
        {
            return new StereoToMonoSampleProvider(source);
        }

        if (channels == 1 && targetChannels == 2)
        {
            return new MonoToStereoSampleProvider(source);
        }

        // ponytail: only 1<->2 conversion supported; multichannel loopback is rare.
        // Upgrade with a MultiplexingSampleProvider downmix if it's ever reported.
        throw new NotSupportedException($"Cannot mix {channels}-channel audio into {targetChannels}-channel output.");
    }

    private void ConfigureSilenceOut(WaveFormat waveFormat)
    {
        // WasapiLoopbackCapture doesn't record any audio when nothing is playing
        // so we must play some silence!

        var silence = new SilenceProvider(waveFormat);
        _silenceWaveOut = new WaveOutEvent();
        _silenceWaveOut.Init(silence);
        _silenceWaveOut.Play();
    }

    /// <summary>
    /// Pauses the current recording. Audio data is discarded until resumed.
    /// </summary>
    public void Pause()
    {
        if (_recordingStatus == RecordingStatus.Recording)
        {
            _isPaused = true;

            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Paused)
            {
                TempRecordingPath = _tempRecordingFilePath,
                FinalRecordingPath = _finalRecordingFilePath,
            });
        }
    }

    /// <summary>
    /// Resumes a paused recording.
    /// </summary>
    public void Resume()
    {
        if (_recordingStatus == RecordingStatus.Paused)
        {
            _isPaused = false;

            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.Recording)
            {
                TempRecordingPath = _tempRecordingFilePath,
                FinalRecordingPath = _finalRecordingFilePath,
            });
        }
    }

    /// <summary>
    /// Stop recording.
    /// </summary>
    /// <param name="fadeOut">true - fade out the recording instead of stopping immediately.</param>
    public void Stop(bool fadeOut)
    {
        if (_recordingStatus is RecordingStatus.Recording or RecordingStatus.Paused)
        {
            var wasPaused = _isPaused;
            _isPaused = false;

            OnRecordingStatusChangeEvent(new RecordingStatusChangeEventArgs(RecordingStatus.StopRequested)
            {
                TempRecordingPath = _tempRecordingFilePath,
                FinalRecordingPath = _finalRecordingFilePath,
            });

            if (fadeOut && !wasPaused)
            {
                _fader?.Start();
            }
            else
            {
                StopCaptures();
            }
        }
    }

    private void StopCaptures()
    {
        _waveSource?.StopRecording();
        _micCapture?.StopRecording();
        _loopbackCapture?.StopRecording();
        _silenceWaveOut?.Stop();
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
        // "None" selected (loopback-only): no microphone needed, so skip the input-device guard.
        if (recordingConfig.RecordingDevice == RecordingConfig.EmptyRecordingDeviceId)
        {
            return;
        }

        var deviceCount = WaveIn.DeviceCount;
        if (deviceCount == 0)
        {
            throw new NoDevicesException();
        }

        if (recordingConfig.RecordingDevice >= deviceCount)
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
    }

    private void WaveSourceDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
    {
        if (_isPaused)
        {
            return;
        }

        // as audio samples are provided by WaveIn, we hook in here
        // and write them to disk, (encoding to MP3 on the fly if needed)
        var buffer = waveInEventArgs.Buffer;
        var bytesRecorded = waveInEventArgs.BytesRecorded;

        var isFloatingPointAudio = _waveSource?.WaveFormat.BitsPerSample == 32;

        if (_fader?.Active == true)
        {
            // we're fading out...
            _fader.FadeBuffer(buffer, bytesRecorded, isFloatingPointAudio);
        }

        AddToSampleAggregator(buffer, bytesRecorded, isFloatingPointAudio);

        _audioWriter?.Write(buffer, 0, bytesRecorded);
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

    private void MicCaptureDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
    {
        if (_isPaused)
        {
            return;
        }

        // The microphone just fills its buffer; the loopback capture clocks the actual mixing.
        _micBuffer?.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
    }

    private void LoopbackCaptureDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
    {
        if (_isPaused)
        {
            return;
        }

        _loopbackBuffer?.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
        PumpMixedAudio(waveInEventArgs.BytesRecorded);
    }

    // Reads the amount of mixed audio that corresponds to the loopback data just received,
    // keeping the output paced to real time. Reads pull from both source buffers (ReadFully
    // pads brief gaps with silence), the summed result is clamped, faded, metered and written.
    private void PumpMixedAudio(int loopbackBytesRecorded)
    {
        if (_mixer == null || _loopbackCapture == null || _audioWriter == null)
        {
            return;
        }

        var loopbackFormat = _loopbackCapture.WaveFormat;
        var frames = loopbackBytesRecorded / loopbackFormat.BlockAlign;
        var outputFrames = (int)((long)frames * _outputSampleRate / loopbackFormat.SampleRate);
        var sampleCount = outputFrames * _outputChannelCount;
        if (sampleCount <= 0)
        {
            return;
        }

        if (_mixSampleBuffer == null || _mixSampleBuffer.Length < sampleCount)
        {
            _mixSampleBuffer = new float[sampleCount];
        }

        var samplesRead = _mixer.Read(_mixSampleBuffer, 0, sampleCount);
        if (samplesRead <= 0)
        {
            return;
        }

        // Summed sources can exceed the [-1, 1] range, so hard-clamp to prevent wrap-around.
        for (var index = 0; index < samplesRead; ++index)
        {
            var sample = _mixSampleBuffer[index];
            _mixSampleBuffer[index] = sample > 1f ? 1f : sample < -1f ? -1f : sample;
        }

        if (_fader?.Active == true)
        {
            _fader.FadeBuffer(_mixSampleBuffer, samplesRead);
        }

        for (var index = 0; index < samplesRead; ++index)
        {
            _sampleAggregator?.Add(_mixSampleBuffer[index]);
        }

        WriteMixedAsPcm16(_mixSampleBuffer, samplesRead);
    }

    private void WriteMixedAsPcm16(float[] samples, int sampleCount)
    {
        var byteCount = sampleCount * 2;
        if (_mixPcmBuffer == null || _mixPcmBuffer.Length < byteCount)
        {
            _mixPcmBuffer = new byte[byteCount];
        }

        var offset = 0;
        for (var index = 0; index < sampleCount; ++index)
        {
            var value = (short)(samples[index] * 32767f);
            _mixPcmBuffer[offset++] = (byte)(value & 0xFF);
            _mixPcmBuffer[offset++] = (byte)((value >> 8) & 0xFF);
        }

        _audioWriter?.Write(_mixPcmBuffer, 0, byteCount);
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
        StopCaptures();
    }

    private void Cleanup()
    {
        _isPaused = false;

        _audioWriter?.Flush();

        _waveSource?.Dispose();
        _waveSource = null;

        _micCapture?.Dispose();
        _micCapture = null;

        _loopbackCapture?.Dispose();
        _loopbackCapture = null;

        _micBuffer = null;
        _loopbackBuffer = null;
        _mixer = null;

        _silenceWaveOut?.Dispose();
        _silenceWaveOut = null;

        _audioWriter?.Dispose();
        _audioWriter = null;

        if (_fader != null)
        {
            _fader.FadeComplete -= FadeCompleteHandler;
            _fader = null;
        }

        _tempRecordingFilePath = null;
    }

    private void InitFader(int sampleRate)
    {
        // used to optionally fade out a recording
        if (_fader != null)
        {
            _fader.FadeComplete -= FadeCompleteHandler;
        }

        _fader = new VolumeFader(sampleRate);
        _fader.FadeComplete += FadeCompleteHandler;
    }
}