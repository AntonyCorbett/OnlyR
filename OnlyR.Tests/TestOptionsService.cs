#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.IO;
using System.Threading.Tasks;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestOptionsService
{
    [Test]
    public async Task ConstructorCreatesDefaultOptions()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.Options;
        });

        try
        {
            await Assert.That(result).IsNotNull();
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task OptionsNotNullWithUniqueIdentifier()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.Options;
        });

        try
        {
            await Assert.That(result).IsNotNull();
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task GetSupportedMp3BitRatesReturnsItems()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.GetSupportedMp3BitRates();
        });

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Length).IsGreaterThan(0);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task GetSupportedSampleRatesReturnsItems()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.GetSupportedSampleRates();
        });

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Length).IsGreaterThan(0);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task GetSupportedChannelsReturnsItems()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.GetSupportedChannels();
        });

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Length).IsGreaterThan(0);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task CultureGetReturnsCultureFromOptions()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object) { Options = { Culture = "fr-FR" } };
            return svc.Culture;
        });

        try
        {
            await Assert.That(result).IsEqualTo("fr-FR");
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task CultureSetUpdatesCulture()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object) { Culture = "de-DE" };
            return svc.Options.Culture;
        });

        try
        {
            await Assert.That(result).IsEqualTo("de-DE");
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task SaveAndReloadPreservesValues()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object) { Options = { MaxRecordingTimeSeconds = 999 } };
            svc.Save();

            var svc2 = new OptionsService(cmdMock.Object);
            return svc2.Options.MaxRecordingTimeSeconds;
        });

        try
        {
            await Assert.That(result).IsEqualTo(999);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task SaveDoesNotThrow()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var success = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            svc.Save();
            return true;
        });

        try
        {
            await Assert.That(success).IsTrue();
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task DefaultOptionsHaveExpectedDefaults()
    {
        var identifier = $"test_{Guid.NewGuid():N}";

        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns(identifier);

            var svc = new OptionsService(cmdMock.Object);
            return svc.Options;
        });

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.SampleRate).IsGreaterThan(0);
            await Assert.That(result.ChannelCount).IsGreaterThan(0);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
    }

    [Test]
    public async Task ConstructorWithNullIdentifier()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var cmdMock = Mock.Of<ICommandLineService>();
            cmdMock.OptionsIdentifier.Returns((string?)null);

            var svc = new OptionsService(cmdMock.Object);
            return svc.Options;
        });

        await Assert.That(result).IsNotNull();
    }

    private static void CleanupAppDataFolder(string identifier)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OnlyR",
                identifier);

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
