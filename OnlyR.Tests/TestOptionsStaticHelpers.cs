using System.Linq;
using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestOptionsStaticHelpers
{
    [Test]
    public async Task GetSupportedSampleRatesIsNotEmpty()
    {
        var result = Options.GetSupportedSampleRates();
        await Assert.That(result.Count()).IsGreaterThan(0);
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
        await Assert.That(enumerable.Length).IsGreaterThan(0);
        await Assert.That(enumerable.Contains(1)).IsTrue();
        await Assert.That(enumerable.Contains(2)).IsTrue();
    }

    [Test]
    public async Task GetSupportedMp3BitRatesIsNotEmpty()
    {
        var result = Options.GetSupportedMp3BitRates();
        await Assert.That(result.Count()).IsGreaterThan(0);
    }

    [Test]
    public async Task GetSupportedMp3BitRatesContainsDefault()
    {
        var result = Options.GetSupportedMp3BitRates();
        await Assert.That(result.Contains(96)).IsTrue();
    }
}
