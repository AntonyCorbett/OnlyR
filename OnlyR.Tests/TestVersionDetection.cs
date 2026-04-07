using System;
using System.Threading.Tasks;
using OnlyR.AutoUpdates;

namespace OnlyR.Tests;

public sealed class TestVersionDetection
{
    [Test]
    public async Task ParseVersionStringValidFourPart()
    {
        var result = VersionDetection.ParseVersionString("1.2.3.4");
        await Assert.That(result).IsEqualTo(new Version(1, 2, 3, 4));
    }

    [Test]
    public async Task ParseVersionStringNull()
    {
        var result = VersionDetection.ParseVersionString(null);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringEmpty()
    {
        var result = VersionDetection.ParseVersionString(string.Empty);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringThreeParts()
    {
        var result = VersionDetection.ParseVersionString("1.2.3");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringFiveParts()
    {
        var result = VersionDetection.ParseVersionString("1.2.3.4.5");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringNonNumeric()
    {
        var result = VersionDetection.ParseVersionString("a.b.c.d");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringPartiallyNumeric()
    {
        var result = VersionDetection.ParseVersionString("1.2.x.4");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseVersionStringLargeNumbers()
    {
        var result = VersionDetection.ParseVersionString("10.20.30.40");
        await Assert.That(result).IsEqualTo(new Version(10, 20, 30, 40));
    }

    [Test]
    public async Task ParseVersionStringZeros()
    {
        var result = VersionDetection.ParseVersionString("0.0.0.0");
        await Assert.That(result).IsEqualTo(new Version(0, 0, 0, 0));
    }

    [Test]
    public async Task GetCurrentVersionReturnsNonNull()
    {
        var result = VersionDetection.GetCurrentVersion();
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task LatestReleaseUrlIsNotEmpty() =>
        await Assert.That(VersionDetection.LatestReleaseUrl).IsNotEmpty();
}
