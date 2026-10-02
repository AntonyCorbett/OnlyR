using NAudio.CoreAudioApi;
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
using System.Runtime.InteropServices;
using System.Threading;

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

    private readonly Lock _writerLock = new();
    private Stream? _audioWriter;
    private WaveOut? _silenceWaveOut;
    private SampleAggregator? _sampleAggregator;
    private VolumeFader? _fader;
    private RecordingStatus _recordingStatus;
    private bool _isPaused;
    private string? _tempRecordingFilePath;
    private string? _finalRecordingFilePath;

    private WaveIn? _micCapture;
    private WasapiRecorder? _loopbackCapture;
    private bool _singleSourceIsFloat;
    private byte[]? _loopbackScratchBuffer;

    // Mixed (microphone + loopback) recording path. Only used when both sources are active.
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
        WaveFormat sourceFormat;

        if (recordingConfig.UseLoopbackCapture)
        {
            _loopbackCapture = CreateLoopbackRecorder();
            sourceFormat = _loopbackCapture.WaveFormat.AsStandardWaveFormat();

            ConfigureSilenceOut(sourceFormat);

            _loopbackCapture.DataAvailable += SingleSourceLoopbackDataAvailableHandler;
            _loopbackCapture.RecordingStopped += WaveSourceRecordingStoppedHandler;
        }
        else
        {
            _micCapture = new WaveIn
            {
                WaveFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount),
                DeviceNumber = recordingConfig.RecordingDevice,
            };
            sourceFormat = _micCapture.WaveFormat;

            _micCapture.DataAvailable += SingleSourceMicDataAvailableHandler;
            _micCapture.RecordingStopped += WaveSourceRecordingStoppedHandler;
        }

        _singleSourceIsFloat = sourceFormat.BitsPerSample == 32;

        InitAggregator(sourceFormat.SampleRate);
        InitFader(sourceFormat.SampleRate);

        _audioWriter = CreateAudioWriter(recordingConfig, sourceFormat);

        _micCapture?.StartRecording();
        _loopbackCapture?.StartRecording();
    }

    private static WasapiRecorder CreateLoopbackRecorder() =>
        new WasapiRecorderBuilder().WithLoopbackCapture().Build();

    // Mixed recording: microphone + system loopback combined into a single output stream.
    // Each capture fills its own buffer; reads are clocked off the loopback capture (kept ticking
    // by the silence-out trick) and the two streams are summed via a MixingSampleProvider.
    private void StartMixedRecording(RecordingConfig recordingConfig)
    {
        _outputSampleRate = recordingConfig.SampleRate;
        _outputChannelCount = recordingConfig.ChannelCount;

        // Output matches single-source recordings: 16-bit PCM at the configured rate/channels.
        var outputFormat = new WaveFormat(recordingConfig.SampleRate, recordingConfig.ChannelCount);

        _micCapture = new WaveIn
        {
            WaveFormat = outputFormat,
            DeviceNumber = recordingConfig.RecordingDevice,
        };
        _micBuffer = new BufferedWaveProvider(_micCapture.WaveFormat, TimeSpan.FromSeconds(MixBufferSeconds))
        {
            DiscardOnBufferOverflow = true,
        };
        _micCapture.DataAvailable += MicCaptureDataAvailableHandler;

        _loopbackCapture = CreateLoopbackRecorder();
        _loopbackBuffer = new BufferedWaveProvider(_loopbackCapture.WaveFormat, TimeSpan.FromSeconds(MixBufferSeconds))
        {
            DiscardOnBufferOverflow = true,
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
        // Loopback capture doesn't record any audio when nothing is playing
        // so we must play some silence!

        var silence = new SilenceProvider(waveFormat);
        _silenceWaveOut = new WaveOut();
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

        // Some devices may only support mono capture
        // so clamp to what the device actually reports.
        var maxChannels = WaveIn.GetCapabilities(recordingConfig.RecordingDevice).Channels;
        if (maxChannels > 0 && recordingConfig.ChannelCount > maxChannels)
        {
            recordingConfig.ChannelCount = maxChannels;
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

    private void SingleSourceMicDataAvailableHandler(object? sender, WaveInEventArgs waveInEventArgs)
    {
        if (_isPaused)
        {
            return;
        }

        ProcessSingleSourceBuffer(waveInEventArgs.Buffer.AsSpan(0, waveInEventArgs.BytesRecorded));
    }

    private void SingleSourceLoopbackDataAvailableHandler(
        ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        if (_isPaused)
        {
            return;
        }

        // The WASAPI span is only valid for the duration of this callback, and the fader
        // modifies samples in place, so take a copy before processing.
        ProcessSingleSourceBuffer(CopyToScratchBuffer(buffer));
    }

    // we hook in here and write the samples to disk, (encoding to MP3 on the fly if needed)
    private void ProcessSingleSourceBuffer(Span<byte> buffer)
    {
        if (_fader?.Active == true)
        {
            // we're fading out...
            _fader.FadeBuffer(buffer, _singleSourceIsFloat);
        }

        AddToSampleAggregator(buffer, _singleSourceIsFloat);

        lock (_writerLock)
        {
            _audioWriter?.Write(buffer);
        }
    }

    private Span<byte> CopyToScratchBuffer(ReadOnlySpan<byte> buffer)
    {
        if (_loopbackScratchBuffer == null || _loopbackScratchBuffer.Length < buffer.Length)
        {
            _loopbackScratchBuffer = new byte[buffer.Length];
        }

        var scratch = _loopbackScratchBuffer.AsSpan(0, buffer.Length);
        buffer.CopyTo(scratch);
        return scratch;
    }

    private void AddToSampleAggregator(ReadOnlySpan<byte> buffer, bool isFloatingPointAudio)
    {
        if (isFloatingPointAudio)
        {
            foreach (var sample in MemoryMarshal.Cast<byte, float>(buffer))
            {
                _sampleAggregator?.Add(sample);
            }
        }
        else
        {
            foreach (var sample in MemoryMarshal.Cast<byte, short>(buffer))
            {
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

    private void LoopbackCaptureDataAvailableHandler(
        ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        if (_isPaused)
        {
            return;
        }

        _loopbackBuffer?.AddSamples(buffer);
        PumpMixedAudio(buffer.Length);
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

        var samplesRead = _mixer.Read(_mixSampleBuffer.AsSpan(0, sampleCount));
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
            _fader.FadeBuffer(_mixSampleBuffer.AsSpan(0, samplesRead));
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

        lock (_writerLock)
        {
            _audioWriter?.Write(_mixPcmBuffer, 0, byteCount);
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
        StopCaptures();
    }

    private void Cleanup()
    {
        _isPaused = false;

        // Captures are disposed outside the writer lock: disposal waits for the capture thread,
        // which may be blocked on that lock mid-write.
        _micCapture?.Dispose();
        _micCapture = null;

        _loopbackCapture?.Dispose();
        _loopbackCapture = null;

        _micBuffer = null;
        _loopbackBuffer = null;
        _mixer = null;

        _silenceWaveOut?.Dispose();
        _silenceWaveOut = null;

        // Cleanup runs on both the capture thread (RecordingStopped) and the caller's thread (Dispose),
        // so the writer is detached atomically and only the thread that claims it flushes and disposes it.
        Stream? writer;
        lock (_writerLock)
        {
            writer = _audioWriter;
            _audioWriter = null;
        }

        writer?.Flush();
        writer?.Dispose();

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