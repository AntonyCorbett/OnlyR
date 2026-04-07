using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public class TestOptionsSanitize
{
    [Test]
    public async Task SanitizeMaxRecordingsInOneFolderTooLow()
    {
        var options = new Options { MaxRecordingsInOneFolder = 5 };
        options.Sanitize();
        await Assert.That(options.MaxRecordingsInOneFolder).IsEqualTo(999);
    }

    [Test]
    public async Task SanitizeMaxRecordingsInOneFolderTooHigh()
    {
        var options = new Options { MaxRecordingsInOneFolder = 600 };
        options.Sanitize();
        await Assert.That(options.MaxRecordingsInOneFolder).IsEqualTo(999);
    }

    [Test]
    public async Task SanitizeSilencePercentageTooLow()
    {
        var options = new Options { SilenceAsVolumePercentage = 0 };
        options.Sanitize();
        await Assert.That(options.SilenceAsVolumePercentage).IsEqualTo(5);
    }

    [Test]
    public async Task SanitizeSilencePercentageTooHigh()
    {
        var options = new Options { SilenceAsVolumePercentage = 95 };
        options.Sanitize();
        await Assert.That(options.SilenceAsVolumePercentage).IsEqualTo(5);
    }

    [Test]
    public async Task SanitizeInvalidCodec()
    {
        var options = new Options { Codec = (AudioCodec)99 };
        options.Sanitize();
        await Assert.That(options.Codec).IsEqualTo(AudioCodec.Mp3);
    }

    [Test]
    public async Task SanitizeEmptyGenre()
    {
        var options = new Options { Genre = string.Empty };
        options.Sanitize();
        await Assert.That(options.Genre).IsNotNull();
        await Assert.That(options.Genre).IsNotEmpty();
    }

    [Test]
    public async Task SanitizeNegativeMaxSilenceTimeSeconds()
    {
        var options = new Options { MaxSilenceTimeSeconds = -10 };
        options.Sanitize();
        await Assert.That(options.MaxSilenceTimeSeconds).IsEqualTo(0);
    }

    [Test]
    public async Task SanitizeValidValuesUnchanged()
    {
        var options = new Options
        {
            SampleRate = 22050,
            ChannelCount = 2,
            Mp3BitRate = 128,
            MaxRecordingsInOneFolder = 100,
            SilenceAsVolumePercentage = 50,
            Codec = AudioCodec.Wav,
        };

        options.Sanitize();

        await Assert.That(options.SampleRate).IsEqualTo(22050);
        await Assert.That(options.ChannelCount).IsEqualTo(2);
        await Assert.That(options.Mp3BitRate).IsEqualTo(128);
        await Assert.That(options.MaxRecordingsInOneFolder).IsEqualTo(100);
        await Assert.That(options.SilenceAsVolumePercentage).IsEqualTo(50);
        await Assert.That(options.Codec).IsEqualTo(AudioCodec.Wav);
    }

    [Test]
    public async Task DefaultConstructorProducesValidOptions()
    {
        var options = new Options();

        var maxRecordingsBefore = options.MaxRecordingsInOneFolder;
        var sampleRateBefore = options.SampleRate;
        var channelCountBefore = options.ChannelCount;
        var mp3BitRateBefore = options.Mp3BitRate;
        var genreBefore = options.Genre;
        var maxRecordingTimeBefore = options.MaxRecordingTimeSeconds;
        var recordingDeviceBefore = options.RecordingDevice;
        var silencePercentageBefore = options.SilenceAsVolumePercentage;
        var codecBefore = options.Codec;
        var recordingsLifeTimeBefore = options.RecordingsLifeTimeDays;
        var maxSilenceTimeBefore = options.MaxSilenceTimeSeconds;

        options.Sanitize();

        await Assert.That(options.MaxRecordingsInOneFolder).IsEqualTo(maxRecordingsBefore);
        await Assert.That(options.SampleRate).IsEqualTo(sampleRateBefore);
        await Assert.That(options.ChannelCount).IsEqualTo(channelCountBefore);
        await Assert.That(options.Mp3BitRate).IsEqualTo(mp3BitRateBefore);
        await Assert.That(options.Genre).IsEqualTo(genreBefore);
        await Assert.That(options.MaxRecordingTimeSeconds).IsEqualTo(maxRecordingTimeBefore);
        await Assert.That(options.RecordingDevice).IsEqualTo(recordingDeviceBefore);
        await Assert.That(options.SilenceAsVolumePercentage).IsEqualTo(silencePercentageBefore);
        await Assert.That(options.Codec).IsEqualTo(codecBefore);
        await Assert.That(options.RecordingsLifeTimeDays).IsEqualTo(recordingsLifeTimeBefore);
        await Assert.That(options.MaxSilenceTimeSeconds).IsEqualTo(maxSilenceTimeBefore);
    }
}