using System;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Utils;

namespace OnlyR.Tests;

public sealed class TestEnumExtensionsCodec
{
    [Test]
    public async Task UnknownStatusThrowsArgumentException() =>
        await Assert.That(() => RecordingStatus.Unknown.GetDescriptiveText()).Throws<ArgumentException>();

    [Test]
    public async Task GetExtensionFormatMp3()
    {
        var result = AudioCodec.Mp3.GetExtensionFormat();
        await Assert.That(result).IsEqualTo("mp3");
    }

    [Test]
    public async Task GetExtensionFormatWav()
    {
        var result = AudioCodec.Wav.GetExtensionFormat();
        await Assert.That(result).IsEqualTo("wav");
    }
}