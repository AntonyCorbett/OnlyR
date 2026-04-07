using System.Linq;
using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public class TestOptionsStaticHelpers
{
    [Test]
    public async Task GetSupportedSampleRatesCount()
    {
        var result = Options.GetSupportedSampleRates();
        await Assert.That(result.Count()).IsEqualTo(7);
    }

    [Test]
    public async Task GetSupportedSampleRatesContainsDefault()
    {
        var result = Options.GetSupportedSampleRates();
        await Assert.That(result.Contains(44100)).IsTrue();
    }

    [Test]
    public async Task GetSupportedChannels()
    {
        var result = Options.GetSupportedChannels();
        var enumerable = result as int[] ?? result.ToArray();
        await Assert.That(enumerable.Length).IsEqualTo(2);
        await Assert.That(enumerable.Contains(1)).IsTrue();
        await Assert.That(enumerable.Contains(2)).IsTrue();
    }

    [Test]
    public async Task GetSupportedMp3BitRatesCount()
    {
        var result = Options.GetSupportedMp3BitRates();
        await Assert.That(result.Count()).IsEqualTo(14);
    }

    [Test]
    public async Task GetSupportedMp3BitRatesContainsDefault()
    {
        var result = Options.GetSupportedMp3BitRates();
        await Assert.That(result.Contains(96)).IsTrue();
    }
}