using System;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Core.EventArgs;
using OnlyR.Core.Models;
using OnlyR.Core.Recorder;

namespace OnlyR.Tests;

public sealed class TestCoreDataClasses
{
    // ========================================================================
    // RecordingConfig
    // ========================================================================

    [Test]
    public async Task RecordingConfigDefaultValues()
    {
        var config = new RecordingConfig();
        await Assert.That(config.RecordingDevice).IsEqualTo(0);
        await Assert.That(config.UseLoopbackCapture).IsFalse();
        await Assert.That(config.TrackNumber).IsEqualTo(0);
        await Assert.That(config.DestFilePath).IsNull();
        await Assert.That(config.FinalFilePath).IsNull();
        await Assert.That(config.SampleRate).IsEqualTo(0);
        await Assert.That(config.ChannelCount).IsEqualTo(0);
        await Assert.That(config.Mp3BitRate).IsNull();
        await Assert.That(config.TrackTitle).IsNull();
        await Assert.That(config.AlbumName).IsNull();
        await Assert.That(config.Genre).IsNull();
    }

    [Test]
    public async Task RecordingConfigSetProperties()
    {
        var date = new DateTime(2026, 4, 7);
        var config = new RecordingConfig
        {
            RecordingDevice = 1,
            UseLoopbackCapture = true,
            RecordingDate = date,
            TrackNumber = 5,
            DestFilePath = "C:\\temp\\recording.mp3",
            FinalFilePath = "C:\\recordings\\recording.mp3",
            SampleRate = 44100,
            ChannelCount = 2,
            Mp3BitRate = 128,
            Codec = AudioCodec.Mp3,
            TrackTitle = "Track 5",
            AlbumName = "My Album",
            Genre = "Speech",
        };

        await Assert.That(config.RecordingDevice).IsEqualTo(1);
        await Assert.That(config.UseLoopbackCapture).IsTrue();
        await Assert.That(config.RecordingDate).IsEqualTo(date);
        await Assert.That(config.TrackNumber).IsEqualTo(5);
        await Assert.That(config.DestFilePath).IsEqualTo("C:\\temp\\recording.mp3");
        await Assert.That(config.FinalFilePath).IsEqualTo("C:\\recordings\\recording.mp3");
        await Assert.That(config.SampleRate).IsEqualTo(44100);
        await Assert.That(config.ChannelCount).IsEqualTo(2);
        await Assert.That(config.Mp3BitRate).IsEqualTo(128);
        await Assert.That(config.Codec).IsEqualTo(AudioCodec.Mp3);
        await Assert.That(config.TrackTitle).IsEqualTo("Track 5");
        await Assert.That(config.AlbumName).IsEqualTo("My Album");
        await Assert.That(config.Genre).IsEqualTo("Speech");
    }

    // ========================================================================
    // RecordingProgressEventArgs
    // ========================================================================

    [Test]
    public async Task RecordingProgressEventArgsDefaultValue()
    {
        var args = new RecordingProgressEventArgs();
        await Assert.That(args.VolumeLevelAsPercentage).IsEqualTo(0);
    }

    [Test]
    public async Task RecordingProgressEventArgsSetVolume()
    {
        var args = new RecordingProgressEventArgs { VolumeLevelAsPercentage = 75 };
        await Assert.That(args.VolumeLevelAsPercentage).IsEqualTo(75);
    }

    // ========================================================================
    // RecordingStatusChangeEventArgs
    // ========================================================================

    [Test]
    public async Task RecordingStatusChangeEventArgsStoresStatus()
    {
        var args = new RecordingStatusChangeEventArgs(RecordingStatus.Recording);
        await Assert.That(args.RecordingStatus).IsEqualTo(RecordingStatus.Recording);
    }

    [Test]
    public async Task RecordingStatusChangeEventArgsPathsDefaultNull()
    {
        var args = new RecordingStatusChangeEventArgs(RecordingStatus.NotRecording);
        await Assert.That(args.TempRecordingPath).IsNull();
        await Assert.That(args.FinalRecordingPath).IsNull();
    }

    [Test]
    public async Task RecordingStatusChangeEventArgsSetPaths()
    {
        var args = new RecordingStatusChangeEventArgs(RecordingStatus.Recording)
        {
            TempRecordingPath = "C:\\temp\\file.mp3",
            FinalRecordingPath = "C:\\final\\file.mp3",
        };

        await Assert.That(args.TempRecordingPath).IsEqualTo("C:\\temp\\file.mp3");
        await Assert.That(args.FinalRecordingPath).IsEqualTo("C:\\final\\file.mp3");
    }

    // ========================================================================
    // RecordingDeviceInfo
    // ========================================================================

    [Test]
    public async Task RecordingDeviceInfoStoresId()
    {
        var info = new RecordingDeviceInfo(1, "Microphone");
        await Assert.That(info.Id).IsEqualTo(1);
    }

    [Test]
    public async Task RecordingDeviceInfoStoresName()
    {
        var info = new RecordingDeviceInfo(1, "Microphone");
        await Assert.That(info.Name).IsEqualTo("Microphone");
    }
}
