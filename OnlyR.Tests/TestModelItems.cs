using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Model;

namespace OnlyR.Tests;

public sealed class TestModelItems
{
    // ========================================================================
    // BitRateItem
    // ========================================================================

    [Test]
    public async Task BitRateItemStoresName()
    {
        var item = new BitRateItem("128 kbps", 128);
        await Assert.That(item.Name).IsEqualTo("128 kbps");
    }

    [Test]
    public async Task BitRateItemStoresActualBitRate()
    {
        var item = new BitRateItem("128 kbps", 128);
        await Assert.That(item.ActualBitRate).IsEqualTo(128);
    }

    // ========================================================================
    // ChannelItem
    // ========================================================================

    [Test]
    public async Task ChannelItemStoresName()
    {
        var item = new ChannelItem("Stereo", 2);
        await Assert.That(item.Name).IsEqualTo("Stereo");
    }

    [Test]
    public async Task ChannelItemStoresChannelCount()
    {
        var item = new ChannelItem("Stereo", 2);
        await Assert.That(item.ChannelCount).IsEqualTo(2);
    }

    // ========================================================================
    // CodecItem
    // ========================================================================

    [Test]
    public async Task CodecItemStoresName()
    {
        var item = new CodecItem("MP3", AudioCodec.Mp3);
        await Assert.That(item.Name).IsEqualTo("MP3");
    }

    [Test]
    public async Task CodecItemStoresCodec()
    {
        var item = new CodecItem("WAV", AudioCodec.Wav);
        await Assert.That(item.Codec).IsEqualTo(AudioCodec.Wav);
    }

    // ========================================================================
    // MaxSilenceTimeItem
    // ========================================================================

    [Test]
    public async Task MaxSilenceTimeItemStoresName()
    {
        var item = new MaxSilenceTimeItem("30 seconds", 30);
        await Assert.That(item.Name).IsEqualTo("30 seconds");
    }

    [Test]
    public async Task MaxSilenceTimeItemStoresSeconds()
    {
        var item = new MaxSilenceTimeItem("30 seconds", 30);
        await Assert.That(item.Seconds).IsEqualTo(30);
    }

    // ========================================================================
    // RecordingDeviceItem
    // ========================================================================

    [Test]
    public async Task RecordingDeviceItemStoresDeviceId()
    {
        var item = new RecordingDeviceItem(3, "Microphone");
        await Assert.That(item.DeviceId).IsEqualTo(3);
    }

    [Test]
    public async Task RecordingDeviceItemStoresDeviceName()
    {
        var item = new RecordingDeviceItem(3, "Microphone");
        await Assert.That(item.DeviceName).IsEqualTo("Microphone");
    }

    // ========================================================================
    // RecordingLifeTimeItem
    // ========================================================================

    [Test]
    public async Task RecordingLifeTimeItemStoresDescription()
    {
        var item = new RecordingLifeTimeItem("1 week", 7);
        await Assert.That(item.Description).IsEqualTo("1 week");
    }

    [Test]
    public async Task RecordingLifeTimeItemStoresDays()
    {
        var item = new RecordingLifeTimeItem("1 week", 7);
        await Assert.That(item.Days).IsEqualTo(7);
    }

    // ========================================================================
    // SampleRateItem
    // ========================================================================

    [Test]
    public async Task SampleRateItemStoresName()
    {
        var item = new SampleRateItem("44.1 kHz", 44100);
        await Assert.That(item.Name).IsEqualTo("44.1 kHz");
    }

    [Test]
    public async Task SampleRateItemStoresActualSampleRate()
    {
        var item = new SampleRateItem("44.1 kHz", 44100);
        await Assert.That(item.ActualSampleRate).IsEqualTo(44100);
    }

    // ========================================================================
    // MaxRecordingTimeItem
    // ========================================================================

    [Test]
    public async Task MaxRecordingTimeItemStoresName()
    {
        var item = new MaxRecordingTimeItem("5 mins", 300);
        await Assert.That(item.Name).IsEqualTo("5 mins");
    }

    [Test]
    public async Task MaxRecordingTimeItemStoresActualSeconds()
    {
        var item = new MaxRecordingTimeItem("5 mins", 300);
        await Assert.That(item.ActualSeconds).IsEqualTo(300);
    }

    // ========================================================================
    // LanguageItem
    // ========================================================================

    [Test]
    public async Task LanguageItemStoresLanguageId()
    {
        var item = new LanguageItem("en-GB", "English");
        await Assert.That(item.LanguageId).IsEqualTo("en-GB");
    }

    [Test]
    public async Task LanguageItemStoresLanguageName()
    {
        var item = new LanguageItem("en-GB", "English");
        await Assert.That(item.LanguageName).IsEqualTo("English");
    }
}
