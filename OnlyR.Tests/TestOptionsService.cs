#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OnlyR.Model;
using OnlyR.Services.Options;

namespace OnlyR.Tests;

public sealed class TestOptionsService
{
    [Test]
    public async Task ConstructorCreatesDefaultOptions()
    {
        var identifier = $"test_{Guid.NewGuid():N}";
        Options? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                result = svc.Options;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

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
        Options? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                result = svc.Options;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

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
        BitRateItem[]? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                result = svc.GetSupportedMp3BitRates();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Length).IsGreaterThan(0);
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
        SampleRateItem[]? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                result = svc.GetSupportedSampleRates();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Length).IsGreaterThan(0);
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
        ChannelItem[]? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                result = svc.GetSupportedChannels();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        try
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Length).IsGreaterThan(0);
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
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                svc.Options.Culture = "fr-FR";
                result = svc.Culture;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

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
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                svc.Culture = "de-DE";
                result = svc.Options.Culture;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

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
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var cmdMock = Mock.Of<ICommandLineService>();
                cmdMock.OptionsIdentifier.Returns(identifier);

                var svc = new OptionsService(cmdMock.Object);
                svc.Options.MaxRecordingTimeSeconds = 999;
                svc.Save();

                var svc2 = new OptionsService(cmdMock.Object);
                result = svc2.Options.MaxRecordingTimeSeconds;
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        await tcs.Task;

        try
        {
            await Assert.That(result).IsEqualTo(999);
        }
        finally
        {
            CleanupAppDataFolder(identifier);
        }
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
