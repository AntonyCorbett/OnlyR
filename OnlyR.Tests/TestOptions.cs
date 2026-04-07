using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests
{
    public class TestOptions
    {
        private const int BadIntValue = -100;

        [Test]
        public async Task TestSanitize()
        {
            var options = new Options
            {
                ChannelCount = BadIntValue,
                MaxRecordingTimeSeconds = BadIntValue,
                MaxRecordingsInOneFolder = BadIntValue,
                Mp3BitRate = BadIntValue,
                RecordingDevice = BadIntValue,
                SampleRate = BadIntValue,
            };

            options.Sanitize();

            await Assert.That(options.ChannelCount).IsNotEqualTo(BadIntValue);
            await Assert.That(options.MaxRecordingTimeSeconds).IsNotEqualTo(BadIntValue);
            await Assert.That(options.MaxRecordingsInOneFolder).IsNotEqualTo(BadIntValue);
            await Assert.That(options.Mp3BitRate).IsNotEqualTo(BadIntValue);
            await Assert.That(options.RecordingDevice).IsNotEqualTo(BadIntValue);
            await Assert.That(options.SampleRate).IsNotEqualTo(BadIntValue);
        }
    }
}
