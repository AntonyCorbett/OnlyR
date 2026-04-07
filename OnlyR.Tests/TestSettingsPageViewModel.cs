#pragma warning disable CA1416 // Validate platform compatibility

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlyR.Core.Enums;
using OnlyR.Model;
using OnlyR.Services.Options;
using OnlyR.Tests.Mocks;
using OnlyR.ViewModel;

namespace OnlyR.Tests;

public sealed class TestSettingsPageViewModel
{
    [Test]
    [NotInParallel("Messenger")]
    public async Task MaxRecordingTimesHasExpectedCount()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.MaxRecordingTimes.Count();
        });

        await Assert.That(result).IsEqualTo(13);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task MaxRecordingTimesIncludesNoLimit()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.MaxRecordingTimes.First().ActualSeconds;
        });

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RecordingLifeTimesHasExpectedCount()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.RecordingLifeTimes.Count();
        });

        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task MaxSilenceTimeItemsHasExpectedCount()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.MaxSilenceTimeItems.Count();
        });

        await Assert.That(result).IsEqualTo(8);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CodecsIncludesMp3AndWav()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.Codecs.Count();
        });

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task VersionStringNotEmpty()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.AppVersionStr;
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsNotEmpty();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CodecMp3ShowsBitRate()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel(new Options { Codec = AudioCodec.Mp3 });
            return vm.ShowBitRate;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CodecWavHidesBitRate()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Codec = AudioCodec.Wav;
            return vm.ShowBitRate;
        });

        await Assert.That(result).IsFalse();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task MaxSilenceTimeSecondsRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.MaxSilenceTimeSeconds = 30;
            return vm.MaxSilenceTimeSeconds;
        });

        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SilenceAsVolumePercentageRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.SilenceAsVolumePercentage = 10;
            return vm.SilenceAsVolumePercentage;
        });

        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task MaxRecordingTimeRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.MaxRecordingTime = 600;
            return vm.MaxRecordingTime;
        });

        await Assert.That(result).IsEqualTo(600);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RecordingLifeTimeRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingLifeTime = 7;
            return vm.RecordingLifeTime;
        });

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ShouldFadeRecordingsMatchesOptions()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.ShouldFadeRecordings = true;
            return vm.ShouldFadeRecordings;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ShowPauseRecordingButtonMatchesOptions()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.ShowPauseRecordingButton = true;
            return vm.ShowPauseRecordingButton;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task StartRecordingOnLaunchMatchesOptions()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.StartRecordingOnLaunch = true;
            return vm.StartRecordingOnLaunch;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task AlwaysOnTopSetterUpdatesValue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.AlwaysOnTop = true;
            return vm.AlwaysOnTop;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task StartMinimizedMatchesOptions()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.StartMinimized = true;
            return vm.StartMinimized;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task AllowCloseWhenRecordingMatchesOptions()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.AllowCloseWhenRecording = true;
            return vm.AllowCloseWhenRecording;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task UseLoopbackCaptureUpdatesNotUsingLoopback()
    {
        var notUsingLoopback = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.UseLoopbackCapture = true;
            return vm.NotUsingLoopbackCapture;
        });

        await Assert.That(notUsingLoopback).IsFalse();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task GenreRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Genre = "Music";
            return vm.Genre;
        });

        await Assert.That(result).IsEqualTo("Music");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task LanguagesListIsPopulated()
    {
        var count = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.Languages.Count();
        });

        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task PageNameIsSettings() =>
        await Assert.That(SettingsPageViewModel.PageName).IsEqualTo("SettingsPage");

    [Test]
    [NotInParallel("Messenger")]
    public async Task DestinationFolderRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.DestinationFolder = "C:\\TestFolder";
            return vm.DestinationFolder;
        });

        await Assert.That(result).IsEqualTo("C:\\TestFolder");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task CodecSetterFiresPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != null)
                {
                    props.Add(args.PropertyName);
                }
            };
            vm.Codec = AudioCodec.Wav;
            return props;
        });

        await Assert.That(changedProperties).Contains("ShowBitRate");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task NavigateRecordingCommandIsNotNull()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.NavigateRecordingCommand != null;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SampleRateRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.SampleRate = 44100;
            return vm.SampleRate;
        });

        await Assert.That(result).IsEqualTo(44100);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ChannelRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Channel = 2;
            return vm.Channel;
        });

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task BitRateRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.BitRate = 128;
            return vm.BitRate;
        });

        await Assert.That(result).IsEqualTo(128);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RecordingDeviceIdRoundTrips()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.RecordingDeviceId = 2;
            return vm.RecordingDeviceId;
        });

        await Assert.That(result).IsEqualTo(2);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task RecordingDevicesReturnsItems()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.RecordingDevices.Count();
        });

        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SampleRatesReturnsItems()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.SampleRates.Count();
        });

        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ChannelsReturnsItems()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.Channels.Count();
        });

        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task BitRatesReturnsItems()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.BitRates.Count();
        });

        await Assert.That(result).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ActivatedDoesNotThrow()
    {
        await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            vm.Activated(null);
        });
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task LanguageIdSetterFiresPropertyChanged()
    {
        var changedProperties = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            var props = new List<string>();
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != null)
                {
                    props.Add(args.PropertyName);
                }
            };
            vm.LanguageId = "fr-FR";
            return props;
        });

        await Assert.That(changedProperties!).Contains("LanguageId");
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ShowBitRateInitiallyMatchesCodec()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.ShowBitRate;
        });

        // Default codec is MP3
        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task NotUsingLoopbackCaptureDefaultTrue()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.NotUsingLoopbackCapture;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task ShowRecordingsCommandIsNotNull()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.ShowRecordingsCommand != null;
        });

        await Assert.That(result).IsTrue();
    }

    [Test]
    [NotInParallel("Messenger")]
    public async Task SelectDestinationFolderCommandIsNotNull()
    {
        var result = await StaThreadHelper.RunOnSta(() =>
        {
            var vm = CreateViewModel();
            return vm.SelectDestinationFolderCommand != null;
        });

        await Assert.That(result).IsTrue();
    }

    private static SettingsPageViewModel CreateViewModel(Options? options = null)
    {
        var audioService = new MockAudioService();

        var optionsMock = Mock.Of<IOptionsService>();
        optionsMock.Options.Returns(options ?? new Options());
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
