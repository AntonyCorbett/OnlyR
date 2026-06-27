using NAudio.Wave;
using System;

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
    /// <param name="bytesInBuffer">The number of bytes in the audio buffer.</param>
    /// <param name="isFloatingPointAudio">If the audio is 32-bit.</param>
    public void FadeBuffer(byte[] buffer, int bytesInBuffer, bool isFloatingPointAudio)
    {
        _sampleCountModified += bytesInBuffer;
        var volumeAdjustmentFraction = 1 - ((float)_sampleCountModified / _sampleCountToModify);

        var buff = new WaveBuffer(buffer);

        if (isFloatingPointAudio)
        {
            for (var index = 0; index < bytesInBuffer / 4; ++index)
            {
                var sample = buff.FloatBuffer[index];
                buff.FloatBuffer[index] = sample * volumeAdjustmentFraction;
            }
        }
        else
        {
            for (var index = 0; index < bytesInBuffer / 2; ++index)
            {
                var sample = buff.ShortBuffer[index];
                buff.ShortBuffer[index] = (short)(sample * volumeAdjustmentFraction);
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
    /// <param name="sampleCount">The number of samples to modify.</param>
    public void FadeBuffer(float[] buffer, int sampleCount)
    {
        _sampleCountModified += sampleCount;
        var volumeAdjustmentFraction = 1 - ((float)_sampleCountModified / _sampleCountToModify);

        for (var index = 0; index < sampleCount; ++index)
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