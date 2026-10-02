using System;
using System.Runtime.InteropServices;

namespace OnlyR.Core.Recorder;

/// <summary>
/// Controls optional volume fading at end of a recording
/// </summary>
internal sealed class VolumeFader
{
    private readonly int _sampleRate;
    private readonly int _fadeTimeSecs = 4;
    private int _sampleCountToModify;
    private int _sampleCountModified;

    public VolumeFader(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public event EventHandler? FadeComplete;

    public bool Active { get; private set; }

    /// <summary>
    /// Start to fade out.
    /// </summary>
    public void Start()
    {
        _sampleCountToModify = _fadeTimeSecs * _sampleRate; // num samples to modify in order to fade over _fadeTimeSecs
        _sampleCountModified = 0;
        Active = true;
    }

    /// <summary>
    /// Modifies the audio buffer in accord with the current fading status.
    /// </summary>
    /// <param name="buffer">The audio samples.</param>
    /// <param name="isFloatingPointAudio">If the audio is 32-bit.</param>
    public void FadeBuffer(Span<byte> buffer, bool isFloatingPointAudio)
    {
        _sampleCountModified += buffer.Length;
        var volumeAdjustmentFraction = 1 - ((float)_sampleCountModified / _sampleCountToModify);

        if (isFloatingPointAudio)
        {
            var samples = MemoryMarshal.Cast<byte, float>(buffer);
            for (var index = 0; index < samples.Length; ++index)
            {
                samples[index] *= volumeAdjustmentFraction;
            }
        }
        else
        {
            var samples = MemoryMarshal.Cast<byte, short>(buffer);
            for (var index = 0; index < samples.Length; ++index)
            {
                samples[index] = (short)(samples[index] * volumeAdjustmentFraction);
            }
        }

        if (volumeAdjustmentFraction <= 0)
        {
            OnFadeComplete();
        }
    }

    /// <summary>
    /// Modifies a buffer of float samples in accord with the current fading status.
    /// Used by the mixed (mic + loopback) recording path.
    /// </summary>
    /// <param name="buffer">The interleaved float audio samples.</param>
    public void FadeBuffer(Span<float> buffer)
    {
        _sampleCountModified += buffer.Length;
        var volumeAdjustmentFraction = 1 - ((float)_sampleCountModified / _sampleCountToModify);

        for (var index = 0; index < buffer.Length; ++index)
        {
            buffer[index] *= volumeAdjustmentFraction;
        }

        if (volumeAdjustmentFraction <= 0)
        {
            OnFadeComplete();
        }
    }

    private void OnFadeComplete()
    {
        FadeComplete?.Invoke(this, System.EventArgs.Empty);
        Active = false;
    }
}