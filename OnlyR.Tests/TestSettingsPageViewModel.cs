#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    [Test]
    public async Task CodecMp3ShowsBitRate()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel(new Options { Codec = AudioCodec.Mp3 });
                result = vm.ShowBitRate;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CodecWavHidesBitRate()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.Codec = AudioCodec.Wav;
                result = vm.ShowBitRate;
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

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task MaxSilenceTimeSecondsRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.MaxSilenceTimeSeconds = 30;
                result = vm.MaxSilenceTimeSeconds;
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

        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    public async Task SilenceAsVolumePercentageRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.SilenceAsVolumePercentage = 10;
                result = vm.SilenceAsVolumePercentage;
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
    public async Task MaxRecordingTimeRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.MaxRecordingTime = 600;
                result = vm.MaxRecordingTime;
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

        await Assert.That(result).IsEqualTo(600);
    }

    [Test]
    public async Task RecordingLifeTimeRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingLifeTime = 7;
                result = vm.RecordingLifeTime;
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

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    public async Task ShouldFadeRecordingsMatchesOptions()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.ShouldFadeRecordings = true;
                result = vm.ShouldFadeRecordings;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ShowPauseRecordingButtonMatchesOptions()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.ShowPauseRecordingButton = true;
                result = vm.ShowPauseRecordingButton;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task StartRecordingOnLaunchMatchesOptions()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.StartRecordingOnLaunch = true;
                result = vm.StartRecordingOnLaunch;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AlwaysOnTopSetterUpdatesValue()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.AlwaysOnTop = true;
                result = vm.AlwaysOnTop;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task StartMinimizedMatchesOptions()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.StartMinimized = true;
                result = vm.StartMinimized;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AllowCloseWhenRecordingMatchesOptions()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.AllowCloseWhenRecording = true;
                result = vm.AllowCloseWhenRecording;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UseLoopbackCaptureUpdatesNotUsingLoopback()
    {
        bool? notUsingLoopback = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.UseLoopbackCapture = true;
                notUsingLoopback = vm.NotUsingLoopbackCapture;
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

        await Assert.That(notUsingLoopback).IsFalse();
    }

    [Test]
    public async Task GenreRoundTrips()
    {
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.Genre = "Music";
                result = vm.Genre;
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

        await Assert.That(result).IsEqualTo("Music");
    }

    [Test]
    public async Task LanguagesListIsPopulated()
    {
        int? count = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                count = vm.Languages.Count();
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

        await Assert.That(count!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task PageNameIsSettings() =>
        await Assert.That(SettingsPageViewModel.PageName).IsEqualTo("SettingsPage");

    [Test]
    public async Task DestinationFolderRoundTrips()
    {
        string? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.DestinationFolder = "C:\\TestFolder";
                result = vm.DestinationFolder;
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

        await Assert.That(result).IsEqualTo("C:\\TestFolder");
    }

    [Test]
    public async Task CodecSetterFiresPropertyChanged()
    {
        List<string>? changedProperties = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                changedProperties = [];
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName != null)
                    {
                        changedProperties.Add(args.PropertyName);
                    }
                };
                vm.Codec = AudioCodec.Wav;
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

        await Assert.That(changedProperties).Contains("ShowBitRate");
    }

    [Test]
    public async Task NavigateRecordingCommandIsNotNull()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.NavigateRecordingCommand != null;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task SampleRateRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.SampleRate = 44100;
                result = vm.SampleRate;
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

        await Assert.That(result).IsEqualTo(44100);
    }

    [Test]
    public async Task ChannelRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.Channel = 2;
                result = vm.Channel;
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
    public async Task BitRateRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.BitRate = 128;
                result = vm.BitRate;
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

        await Assert.That(result).IsEqualTo(128);
    }

    [Test]
    public async Task RecordingDeviceIdRoundTrips()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.RecordingDeviceId = 2;
                result = vm.RecordingDeviceId;
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
    public async Task RecordingDevicesReturnsItems()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.RecordingDevices.Count();
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

        await Assert.That(result!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task SampleRatesReturnsItems()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.SampleRates.Count();
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

        await Assert.That(result!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task ChannelsReturnsItems()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.Channels.Count();
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

        await Assert.That(result!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task BitRatesReturnsItems()
    {
        int? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.BitRates.Count();
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

        await Assert.That(result!.Value).IsGreaterThan(0);
    }

    [Test]
    public async Task ActivatedDoesNotThrow()
    {
        bool? success = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                vm.Activated(null);
                success = true;
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

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task LanguageIdSetterFiresPropertyChanged()
    {
        List<string>? changedProperties = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                changedProperties = [];
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName != null)
                    {
                        changedProperties.Add(args.PropertyName);
                    }
                };
                vm.LanguageId = "fr-FR";
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

        await Assert.That(changedProperties!).Contains("LanguageId");
    }

    [Test]
    public async Task ShowBitRateInitiallyMatchesCodec()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.ShowBitRate;
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

        // Default codec is MP3
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NotUsingLoopbackCaptureDefaultTrue()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.NotUsingLoopbackCapture;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ShowRecordingsCommandIsNotNull()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.ShowRecordingsCommand != null;
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

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task SelectDestinationFolderCommandIsNotNull()
    {
        bool? result = null;

        var tcs = new TaskCompletionSource();
        var t = new Thread(() =>
        {
            try
            {
                var vm = CreateViewModel();
                result = vm.SelectDestinationFolderCommand != null;
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