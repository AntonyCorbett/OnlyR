#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OnlyR.Model;
using OnlyR.Services.Options;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests;

public class TestSettingsPageViewModel
{
    [Test]
    public async Task MaxRecordingTimesHasExpectedCount()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.MaxRecordingTimes.Count();
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

        await Assert.That(result).IsEqualTo(13);
    }

    [Test]
    public async Task MaxRecordingTimesIncludesNoLimit()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.MaxRecordingTimes.First().ActualSeconds;
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

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task RecordingLifeTimesHasExpectedCount()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.RecordingLifeTimes.Count();
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

        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    public async Task MaxSilenceTimeItemsHasExpectedCount()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.MaxSilenceTimeItems.Count();
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

        await Assert.That(result).IsEqualTo(8);
    }

    [Test]
    public async Task CodecsIncludesMp3AndWav()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.Codecs.Count();
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

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    public async Task VersionStringNotEmpty()
    {
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.AppVersionStr;
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

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsNotEmpty();
    }

    private static SettingsPageViewModel CreateViewModel()
    {
        var audioService = new MockAudioService();

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(new Options());
        optionsMock.GetSupportedSampleRates().Returns([new SampleRateItem("44.1 kHz", 44100)]);
        optionsMock.GetSupportedChannels().Returns([new ChannelItem("Mono", 1)]);
        optionsMock.GetSupportedMp3BitRates().Returns([new BitRateItem("96 kbps", 96)]);

        var commandLineMock = Mock.Of<ICommandLineService>();

        return new SettingsPageViewModel(
            audioService,
            optionsMock.Object,
            commandLineMock.Object);
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
