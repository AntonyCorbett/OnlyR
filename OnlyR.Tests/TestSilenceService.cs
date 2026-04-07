using System;
using System.Reflection;
using System.Threading.Tasks;
using OnlyR.Services.AudioSilence;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestSilenceService
{
    private static SilenceService CreateService(int silenceAsVolumePercentage = 50)
    {
        var options = new Options { SilenceAsVolumePercentage = silenceAsVolumePercentage };

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(options);

        return new SilenceService(optionsMock.Object);
    }

    private static void SetNonSilenceLastDetected(SilenceService service, DateTime value)
    {
        var field = typeof(SilenceService).GetField(
            "_nonSilenceLastDetected",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(service, value);
    }

    [Test]
    public async Task GetSecondsOfSilenceReturnsZeroInitially()
    {
        var service = CreateService();

        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ReportVolumeAboveThresholdSetsTimestamp()
    {
        var service = CreateService(silenceAsVolumePercentage: 50);

        service.ReportVolume(60);
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ReportVolumeBelowThresholdDoesNotSetTimestamp()
    {
        var service = CreateService(silenceAsVolumePercentage: 50);

        service.ReportVolume(30);
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ReportVolumeAtExactThresholdDoesNotUpdate()
    {
        var service = CreateService(silenceAsVolumePercentage: 50);

        service.ReportVolume(50);
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ResetSetsTimestamp()
    {
        var service = CreateService();

        service.Reset();
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task GetSecondsOfSilenceReturnsPositiveAfterDelay()
    {
        var service = CreateService();

        SetNonSilenceLastDetected(service, DateTime.UtcNow.AddSeconds(-5));
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsGreaterThanOrEqualTo(4);
    }

    [Test]
    public async Task ReportVolumeJustAboveThresholdUpdates()
    {
        var service = CreateService(silenceAsVolumePercentage: 50);

        service.ReportVolume(51);
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ResetThenGetSecondsReturnsZero()
    {
        var service = CreateService();

        service.Reset();
        var result = service.GetSecondsOfSilence();

        await Assert.That(result).IsEqualTo(0);
    }
}
