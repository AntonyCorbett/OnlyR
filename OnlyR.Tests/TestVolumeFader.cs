using System;
using System.Threading.Tasks;
using OnlyR.Core.Recorder;

namespace OnlyR.Tests;

public sealed class TestVolumeFader
{
    [Test]
    public async Task ConstructorSetsInactive()
    {
        var fader = new VolumeFader(44100);
        await Assert.That(fader.Active).IsFalse();
    }

    [Test]
    public async Task StartSetsActive()
    {
        var fader = new VolumeFader(44100);
        fader.Start();
        await Assert.That(fader.Active).IsTrue();
    }

    [Test]
    public async Task FadeFloatBufferReducesVolume()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        const int sampleCount = 10;
        var buffer = new byte[sampleCount * 4];
        for (var i = 0; i < sampleCount; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, buffer.Length, isFloatingPointAudio: true);

        var reduced = false;
        for (var i = 0; i < sampleCount; i++)
        {
            if (BitConverter.ToSingle(buffer, i * 4) < 1.0f)
            {
                reduced = true;
                break;
            }
        }

        await Assert.That(reduced).IsTrue();
    }

    [Test]
    public async Task FadeFloatBufferProgressivelyReduces()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        const int sampleCount = 10;
        var buffer1 = new byte[sampleCount * 4];
        for (var i = 0; i < sampleCount; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer1, i * 4, 4);
        }

        fader.FadeBuffer(buffer1, buffer1.Length, isFloatingPointAudio: true);
        var firstSample1 = BitConverter.ToSingle(buffer1, 0);

        var buffer2 = new byte[sampleCount * 4];
        for (var i = 0; i < sampleCount; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer2, i * 4, 4);
        }

        fader.FadeBuffer(buffer2, buffer2.Length, isFloatingPointAudio: true);
        var firstSample2 = BitConverter.ToSingle(buffer2, 0);

        await Assert.That(firstSample2).IsLessThan(firstSample1);
    }

    [Test]
    public async Task FadeFloatBufferCompletionFiresEvent()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var eventFired = false;
        fader.FadeComplete += (_, _) => eventFired = true;

        // _sampleCountToModify = 4 * 100 = 400, need 400 bytes total
        var buffer = new byte[400];
        for (var i = 0; i < 100; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, 400, isFloatingPointAudio: true);

        await Assert.That(eventFired).IsTrue();
    }

    [Test]
    public async Task FadeFloatBufferCompletionSetsInactive()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var buffer = new byte[400];
        for (var i = 0; i < 100; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, 400, isFloatingPointAudio: true);

        await Assert.That(fader.Active).IsFalse();
    }

    [Test]
    public async Task FadePcmBufferReducesVolume()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        const int sampleCount = 10;
        const short originalValue = 30000;
        var buffer = new byte[sampleCount * 2];
        for (var i = 0; i < sampleCount; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(originalValue), 0, buffer, i * 2, 2);
        }

        fader.FadeBuffer(buffer, buffer.Length, isFloatingPointAudio: false);

        var reduced = false;
        for (var i = 0; i < sampleCount; i++)
        {
            if (BitConverter.ToInt16(buffer, i * 2) < originalValue)
            {
                reduced = true;
                break;
            }
        }

        await Assert.That(reduced).IsTrue();
    }

    [Test]
    public async Task FadePcmBufferCompletionFiresEvent()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var eventFired = false;
        fader.FadeComplete += (_, _) => eventFired = true;

        var buffer = new byte[400];
        for (var i = 0; i < 200; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes((short)30000), 0, buffer, i * 2, 2);
        }

        fader.FadeBuffer(buffer, 400, isFloatingPointAudio: false);

        await Assert.That(eventFired).IsTrue();
    }

    [Test]
    public async Task FadeCompleteEventNotFiredBeforeCompletion()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var eventFired = false;
        fader.FadeComplete += (_, _) => eventFired = true;

        var buffer = new byte[100];
        for (var i = 0; i < 25; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, buffer.Length, isFloatingPointAudio: true);

        await Assert.That(eventFired).IsFalse();
    }

    [Test]
    public async Task FadeCompleteEventFiredExactlyOnce()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var eventCount = 0;
        fader.FadeComplete += (_, _) => eventCount++;

        var buffer = new byte[800];
        for (var i = 0; i < 200; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, 800, isFloatingPointAudio: true);

        await Assert.That(eventCount).IsEqualTo(1);
    }

    [Test]
    public async Task NoExceptionWithoutFadeCompleteSubscriber()
    {
        var fader = new VolumeFader(100);
        fader.Start();

        var buffer = new byte[400];
        for (var i = 0; i < 100; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(1.0f), 0, buffer, i * 4, 4);
        }

        fader.FadeBuffer(buffer, 400, isFloatingPointAudio: true);

        await Assert.That(fader.Active).IsFalse();
    }
}
