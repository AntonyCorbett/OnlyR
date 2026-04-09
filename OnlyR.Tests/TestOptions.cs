using System.Threading.Tasks;
using Newtonsoft.Json;
using OnlyR.Model;
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

    [Test]
    public async Task TestLegacyDarkModeFalseMigratesToSystem()
    {
        const string json = """{ "SampleRate": 44100, "AlwaysOnTop": true }""";

        var options = JsonConvert.DeserializeObject<Options>(json);

        await Assert.That(options).IsNotNull();
        await Assert.That(options!.AppTheme).IsNull();

        options.Sanitize();

        await Assert.That(options.AppTheme).IsEqualTo(AppTheme.System);
    }

    [Test]
    public async Task TestLegacyDarkModeTrueMigratesToDark()
    {
        const string json = """{ "DarkMode": true, "SampleRate": 44100 }""";

        var options = JsonConvert.DeserializeObject<Options>(json);

        await Assert.That(options).IsNotNull();
        await Assert.That(options!.AppTheme).IsNull();

        options.Sanitize();

        await Assert.That(options.AppTheme).IsEqualTo(AppTheme.Dark);
    }

    [Test]
    public async Task TestExplicitAppThemePreserved()
    {
        const string json = """{ "DarkMode": false, "AppTheme": 0 }""";

        var options = JsonConvert.DeserializeObject<Options>(json);

        await Assert.That(options).IsNotNull();
        await Assert.That(options!.AppTheme).IsEqualTo(AppTheme.Light);

        options.Sanitize();

        await Assert.That(options.AppTheme).IsEqualTo(AppTheme.Light);
    }

    [Test]
    public async Task TestAppThemeRoundTrips()
    {
        var original = new Options { AppTheme = AppTheme.Dark };

        var json = JsonConvert.SerializeObject(original);
        var restored = JsonConvert.DeserializeObject<Options>(json);

        await Assert.That(restored).IsNotNull();
        await Assert.That(restored!.AppTheme).IsEqualTo(AppTheme.Dark);
    }

    [Test]
    public async Task TestInvalidAppThemeSanitizedToSystem()
    {
        var options = new Options { AppTheme = (AppTheme)99 };

        options.Sanitize();

        await Assert.That(options.AppTheme).IsEqualTo(AppTheme.System);
    }
}
