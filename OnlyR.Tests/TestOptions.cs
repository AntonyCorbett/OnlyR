using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestOptions
{
    [Test]
    public async Task TestSanitize()
    {
        var options = new Options
        {
            ChannelCount = -100,
            MaxRecordingTimeSeconds = -100,
            MaxRecordingsInOneFolder = -100,
            Mp3BitRate = -100,
            RecordingDevice = -100,
            SampleRate = -100,
        };

        options.Sanitize();

        await Assert.That(options.ChannelCount).IsGreaterThan(0);
        await Assert.That(options.MaxRecordingTimeSeconds).IsGreaterThanOrEqualTo(0);
        await Assert.That(options.MaxRecordingsInOneFolder).IsGreaterThan(0);
        await Assert.That(options.Mp3BitRate).IsGreaterThan(0);
        await Assert.That(options.RecordingDevice).IsGreaterThanOrEqualTo(0);
        await Assert.That(options.SampleRate).IsGreaterThan(0);
    }
}
